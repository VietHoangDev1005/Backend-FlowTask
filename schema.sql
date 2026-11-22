-- =============================================================================
-- TaskFlow: PostgreSQL schema, theo SRS v1.1 (PostgreSQL 16 trở lên)
--
-- Cách dùng: chạy một lần trên database TRỐNG.
--   psql -U <user> -d <db> -f schema.sql
-- Toàn bộ file chạy trong một transaction: lỗi ở đâu thì không còn gì được tạo.
--
-- Cần PostgreSQL 15 trở lên (khóa ngoại ON DELETE SET NULL (cột)); SRS yêu cầu 16 trở lên.
--
-- Quy ước:
--   * Tên bảng và cột dạng snake_case, số nhiều (Database First: scaffold tự đổi sang PascalCase trong C#).
--   * Khóa chính là uuid. Mọi thời điểm là timestamptz (UTC), theo BR-GEN-01.
--   * Enum dùng text + CHECK (dễ đổi hơn enum gốc của PostgreSQL).
--   * Xóa mềm: is_deleted + deleted_at (BR-GEN-02). Truy vấn mặc định phải lọc NOT is_deleted.
--   * Các giới hạn số lượng (50 thành viên, 12 cột, 500 thẻ...) KHÔNG đặt ở đây vì
--     SRS yêu cầu nằm trong cấu hình (NFR-SCAL-02); Backend kiểm tra.
-- =============================================================================

BEGIN;

-- ----------------------------------------------------------------------------
-- 0. Extension và hàm hỗ trợ
-- ----------------------------------------------------------------------------
-- Cài vào schema public để tên hàm cố định (kể cả trên Supabase).
CREATE EXTENSION IF NOT EXISTS unaccent WITH SCHEMA public;  -- tìm kiếm không dấu (BR-SRCH-01)
CREATE EXTENSION IF NOT EXISTS pg_trgm  WITH SCHEMA public;  -- chỉ mục GIN cho tìm theo chuỗi con

-- Hàm bọc IMMUTABLE để dùng trong chỉ mục: bỏ dấu, hạ chữ thường, đổi đ/Đ thành d.
CREATE FUNCTION public.f_unaccent(text) RETURNS text
LANGUAGE sql IMMUTABLE PARALLEL SAFE STRICT
AS $$ SELECT pg_catalog.translate(pg_catalog.lower(public.unaccent('public.unaccent'::regdictionary, $1)),
                                  'đĐ', 'dd') $$;

CREATE FUNCTION trg_set_updated_at() RETURNS trigger
LANGUAGE plpgsql AS $$
BEGIN
  NEW.updated_at := now();
  RETURN NEW;
END $$;

-- Phiên bản dòng tăng mỗi lần sửa: chống ghi đè (BR-CRD-09, trả 409 khi lệch).
CREATE FUNCTION trg_bump_row_version() RETURNS trigger
LANGUAGE plpgsql AS $$
BEGIN
  NEW.row_version := OLD.row_version + 1;
  NEW.updated_at  := now();
  RETURN NEW;
END $$;

-- ----------------------------------------------------------------------------
-- 1. Tài khoản, xác thực, OTP, thiết bị push (trong bảng users) và phiên đăng nhập (refresh_tokens)
-- ----------------------------------------------------------------------------
CREATE TABLE users (
  id                    uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
  email                 text        NOT NULL,
  email_confirmed_at    timestamptz,                      -- NULL = chưa xác thực OTP (BR-ACC-09)
  is_verified           boolean     GENERATED ALWAYS AS (email_confirmed_at IS NOT NULL) STORED,  -- dạng true/false cho dễ đọc, tự suy ra
  display_name          text        NOT NULL,         -- tên đồng đội nhìn thấy và dùng để @nhắc tên
  full_name             text,                             -- họ tên đầy đủ (tùy chọn)
  phone                 text,                             -- số điện thoại, chỉ chữ số, có thể bắt đầu bằng + (tùy chọn)
  birthday              date,                             -- ngày sinh (tùy chọn)
  avatar_url            text,
  avatar_public_id      text,                             -- id ảnh trên Cloudinary, để xóa ảnh cũ (BR-ACC-13)
  time_zone_id          text,                             -- tên IANA, ví dụ Asia/Ho_Chi_Minh (BR-GEN-08)
  password_hash         text,                             -- băm của ASP.NET Core Identity (BR-ACC-04); NULL nếu chỉ đăng nhập bằng Google/Facebook
  provider              text,                             -- 'google' hoặc 'facebook'; NULL = chỉ đăng nhập bằng email và mật khẩu
  provider_user_id      text,                             -- ID người dùng phía Google/Facebook (một tài khoản liên kết một nhà cung cấp)
  access_failed_count   integer     NOT NULL DEFAULT 0,   -- đăng nhập sai liên tiếp (BR-ACC-07)
  lockout_end           timestamptz,                      -- khóa tạm thời 15 phút (BR-ACC-07)
  last_login_at         timestamptz,                      -- lần đăng nhập gần nhất
  -- Thiết bị nhận push (FCM / APNs): mỗi người một thiết bị gần nhất; một thiết bị chỉ thuộc một người.
  push_token            text,
  push_platform         text,
  -- OTP email (mã, số lần gửi, số lần nhập sai) và khóa chặn email KHÔNG lưu ở đây mà ở Redis (xem README):
  -- dữ liệu tạm, Redis tự hết hạn và có sẵn bộ đếm.
  -- Bật tắt từng loại thông báo (FR-NTF-04): khóa là loại thông báo, giá trị true hoặc false
  notification_settings jsonb       NOT NULL
    DEFAULT '{"card_assigned": true, "mentioned": true, "due_soon": true, "comment_added": true, "note_updated": true}'::jsonb,
  created_at            timestamptz NOT NULL DEFAULT now(),
  updated_at            timestamptz NOT NULL DEFAULT now(),
  CONSTRAINT ck_users_email_format CHECK (email ~* '^[^@\s]+@[^@\s]+\.[^@\s]+$'),
  CONSTRAINT ck_users_display_name_len CHECK (char_length(btrim(display_name)) BETWEEN 2 AND 50),  -- BR-ACC-03
  CONSTRAINT ck_users_full_name_len CHECK (full_name IS NULL OR char_length(btrim(full_name)) BETWEEN 2 AND 100),
  CONSTRAINT ck_users_phone CHECK (phone IS NULL OR phone ~ '^\+?[0-9]{8,15}$'),
  -- Chặn năm vô lý; việc không được ở tương lai do Backend kiểm tra
  CONSTRAINT ck_users_birthday CHECK (birthday IS NULL OR birthday BETWEEN DATE '1900-01-01' AND DATE '2100-01-01'),
  CONSTRAINT ck_users_counters CHECK (access_failed_count >= 0),
  CONSTRAINT ck_users_provider CHECK ((provider IS NULL AND provider_user_id IS NULL)
                                      OR (provider IN ('google', 'facebook') AND provider_user_id IS NOT NULL AND btrim(provider_user_id) <> '')),
  CONSTRAINT ck_users_login_method CHECK (password_hash IS NOT NULL OR provider IS NOT NULL),
  CONSTRAINT ck_users_push CHECK ((push_token IS NULL AND push_platform IS NULL)
                                  OR (push_token IS NOT NULL AND push_platform IS NOT NULL AND push_platform IN ('ios', 'android'))),
  CONSTRAINT ck_users_notification_settings CHECK (jsonb_typeof(notification_settings) = 'object')
);
-- Email duy nhất, không phân biệt hoa thường (BR-ACC-01)
CREATE UNIQUE INDEX ux_users_email ON users (lower(email));
-- Một tài khoản Google/Facebook chỉ gắn với một người dùng
CREATE UNIQUE INDEX ux_users_provider ON users (provider, provider_user_id) WHERE provider IS NOT NULL;
-- Một thiết bị chỉ gắn với một người dùng
CREATE UNIQUE INDEX ux_users_push_token ON users (push_token) WHERE push_token IS NOT NULL;
-- Tiến trình nền xóa tài khoản chưa xác thực quá 12 giờ (BR-ACC-12)
CREATE INDEX ix_users_unverified ON users (created_at) WHERE email_confirmed_at IS NULL;

CREATE TABLE refresh_tokens (
  id                   uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id              uuid        NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  session_id           uuid        NOT NULL,           -- một phiên đăng nhập; dùng lại token cũ thì thu hồi cả phiên (BR-ACC-06)
  client               text        NOT NULL,           -- web hoặc mobile (BR-ACC-16)
  token_hash           text        NOT NULL,           -- chỉ lưu bản băm, không lưu token thô
  expires_at           timestamptz NOT NULL,           -- 7 ngày (BR-ACC-05)
  revoked_at           timestamptz,
  replaced_by_token_id uuid        REFERENCES refresh_tokens(id) ON DELETE SET NULL,
  created_at           timestamptz NOT NULL DEFAULT now(),
  CONSTRAINT ck_refresh_tokens_client CHECK (client IN ('web', 'mobile')),
  CONSTRAINT ux_refresh_tokens_hash UNIQUE (token_hash)
);
CREATE INDEX ix_refresh_tokens_user ON refresh_tokens (user_id);
CREATE INDEX ix_refresh_tokens_session ON refresh_tokens (session_id);

-- ----------------------------------------------------------------------------
-- 2. Workspace, thành viên, lời mời
-- ----------------------------------------------------------------------------
CREATE TABLE workspaces (
  id            uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
  name          text        NOT NULL,
  description   text,
  created_by_id uuid        REFERENCES users(id) ON DELETE SET NULL,   -- chỉ ghi nhận người tạo; Owner nằm ở workspace_members
  is_personal   boolean     NOT NULL DEFAULT false,                    -- Personal Workspace (BR-PWS)
  chat_enabled  boolean     NOT NULL DEFAULT true,                     -- Owner bật/tắt kênh chat của nhóm (bỏ qua với Personal Workspace)
  is_deleted    boolean     NOT NULL DEFAULT false,
  deleted_at    timestamptz,
  created_at    timestamptz NOT NULL DEFAULT now(),
  updated_at    timestamptz NOT NULL DEFAULT now(),
  CONSTRAINT ck_workspaces_name_len CHECK (char_length(btrim(name)) BETWEEN 3 AND 60),      -- BR-WS-02
  CONSTRAINT ck_workspaces_soft_delete CHECK (is_deleted = (deleted_at IS NOT NULL))
);
-- Mỗi người dùng đúng một Personal Workspace (BR-PWS-01)
CREATE UNIQUE INDEX ux_workspaces_personal ON workspaces (created_by_id) WHERE is_personal;
-- Tên workspace nhóm không trùng trong các workspace do cùng một người tạo; dữ liệu xóa mềm không chiếm tên (BR-WS-02, BR-GEN-02)
CREATE UNIQUE INDEX ux_workspaces_creator_name ON workspaces (created_by_id, lower(name))
  WHERE NOT is_deleted AND NOT is_personal;

CREATE TABLE workspace_members (
  workspace_id uuid        NOT NULL REFERENCES workspaces(id) ON DELETE CASCADE,
  user_id      uuid        NOT NULL REFERENCES users(id)      ON DELETE CASCADE,
  role         text        NOT NULL,
  joined_at    timestamptz NOT NULL DEFAULT now(),
  PRIMARY KEY (workspace_id, user_id),
  CONSTRAINT ck_workspace_members_role CHECK (role IN ('owner', 'member', 'viewer'))
);
CREATE INDEX ix_workspace_members_user ON workspace_members (user_id);

-- Bảo vệ ở tầng CSDL (kiểm tra cuối giao dịch):
--   * Workspace nhóm luôn có ít nhất một Owner (BR-WS-01, BR-WS-05).
--   * Personal Workspace chỉ có một thành viên (BR-PWS-02).
-- Workspace đã xóa mềm hoặc đã bị xóa hẳn thì bỏ qua.
CREATE FUNCTION trg_workspace_guard() RETURNS trigger
LANGUAGE plpgsql AS $$
DECLARE
  ws_id uuid;
  ws    record;
BEGIN
  IF TG_TABLE_NAME = 'workspaces' THEN
    ws_id := NEW.id;
  ELSIF TG_OP = 'DELETE' THEN
    ws_id := OLD.workspace_id;
  ELSE
    ws_id := NEW.workspace_id;
  END IF;

  SELECT is_personal, is_deleted INTO ws FROM workspaces WHERE id = ws_id;
  IF NOT FOUND OR ws.is_deleted THEN
    RETURN NULL;
  END IF;

  IF NOT EXISTS (SELECT 1 FROM workspace_members WHERE workspace_id = ws_id AND role = 'owner') THEN
    RAISE EXCEPTION 'workspace.last_owner: workspace % phải còn ít nhất một Owner', ws_id
      USING ERRCODE = '23514';
  END IF;

  IF ws.is_personal AND (SELECT count(*) FROM workspace_members WHERE workspace_id = ws_id) > 1 THEN
    RAISE EXCEPTION 'workspace.personal_locked: Personal Workspace % chỉ có một thành viên', ws_id
      USING ERRCODE = '23514';
  END IF;
  RETURN NULL;
END $$;

CREATE CONSTRAINT TRIGGER trg_workspace_members_guard
  AFTER INSERT OR UPDATE OR DELETE ON workspace_members
  DEFERRABLE INITIALLY DEFERRED
  FOR EACH ROW EXECUTE FUNCTION trg_workspace_guard();

CREATE CONSTRAINT TRIGGER trg_workspaces_guard
  AFTER INSERT ON workspaces
  DEFERRABLE INITIALLY DEFERRED
  FOR EACH ROW EXECUTE FUNCTION trg_workspace_guard();

-- Xóa tài khoản (BR-ACC-14): Personal Workspace bị xóa cùng tài khoản.
-- Workspace nhóm mà người đó là Owner duy nhất thì Backend phải thăng Owner khác hoặc xóa workspace trước,
-- nếu không trg_workspace_guard sẽ từ chối khi commit.
CREATE FUNCTION trg_users_before_delete() RETURNS trigger
LANGUAGE plpgsql AS $$
BEGIN
  DELETE FROM workspaces WHERE is_personal AND created_by_id = OLD.id;
  RETURN OLD;
END $$;

CREATE TRIGGER trg_users_before_delete
  BEFORE DELETE ON users
  FOR EACH ROW EXECUTE FUNCTION trg_users_before_delete();

CREATE TABLE invitations (
  id            uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
  workspace_id  uuid        NOT NULL REFERENCES workspaces(id) ON DELETE CASCADE,
  email         text        NOT NULL,
  role          text        NOT NULL,
  token_hash    text        NOT NULL,                  -- chỉ lưu bản băm của token trong link
  invited_by_id uuid        REFERENCES users(id) ON DELETE SET NULL,
  expires_at    timestamptz NOT NULL,                  -- hiệu lực 7 ngày (BR-WS-07)
  accepted_at   timestamptz,                           -- dùng được một lần
  created_at    timestamptz NOT NULL DEFAULT now(),
  updated_at    timestamptz NOT NULL DEFAULT now(),
  CONSTRAINT ck_invitations_role CHECK (role IN ('member', 'viewer')),   -- không mời thẳng Owner (BR-WS-06)
  CONSTRAINT ux_invitations_token UNIQUE (token_hash)
);
-- Mỗi email chỉ có tối đa một lời mời đang chờ trong cùng workspace (BR-WS-08)
CREATE UNIQUE INDEX ux_invitations_pending ON invitations (workspace_id, lower(email))
  WHERE accepted_at IS NULL;

-- ----------------------------------------------------------------------------
-- 3. Board, cột, thẻ
-- ----------------------------------------------------------------------------
CREATE TABLE boards (
  id            uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
  workspace_id  uuid        NOT NULL REFERENCES workspaces(id) ON DELETE CASCADE,
  name          text        NOT NULL,
  created_by_id uuid        REFERENCES users(id) ON DELETE SET NULL,
  is_archived   boolean     NOT NULL DEFAULT false,    -- lưu trữ thay vì xóa (FR-BRD-05, P3)
  description   text,                                  -- mô tả board
  background_color     text,                           -- màu nền (10 màu cố định, như nhãn); chỉ chọn màu hoặc ảnh
  background_image_url text,                           -- ảnh nền (Cloudinary)
  is_deleted    boolean     NOT NULL DEFAULT false,
  deleted_at    timestamptz,
  created_at    timestamptz NOT NULL DEFAULT now(),
  updated_at    timestamptz NOT NULL DEFAULT now(),
  CONSTRAINT ck_boards_name_len CHECK (char_length(btrim(name)) BETWEEN 1 AND 80),    -- BR-BRD-01
  CONSTRAINT ck_boards_description_len CHECK (description IS NULL OR char_length(description) <= 2000),
  CONSTRAINT ck_boards_background_color CHECK (background_color IS NULL OR background_color IN ('red', 'orange', 'yellow', 'green', 'teal', 'blue', 'indigo', 'purple', 'pink', 'gray')),
  CONSTRAINT ck_boards_background_one CHECK (background_color IS NULL OR background_image_url IS NULL),
  CONSTRAINT ck_boards_soft_delete CHECK (is_deleted = (deleted_at IS NOT NULL))
);
CREATE UNIQUE INDEX ux_boards_workspace_name ON boards (workspace_id, lower(name)) WHERE NOT is_deleted;
CREATE INDEX ix_boards_workspace ON boards (workspace_id) WHERE NOT is_deleted;

CREATE TABLE board_columns (
  id          uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
  board_id    uuid        NOT NULL REFERENCES boards(id) ON DELETE CASCADE,
  name        text        NOT NULL,
  position    text        COLLATE "C" NOT NULL,        -- khóa sắp xếp dạng chuỗi (kiểu LexoRank)
  is_done     boolean     NOT NULL DEFAULT false,      -- cột hoàn thành: thẻ trong đó không bị quá hạn (BR-BRD-05, BR-BRD-08)
  is_deleted  boolean     NOT NULL DEFAULT false,
  deleted_at  timestamptz,
  is_archived boolean     NOT NULL DEFAULT false,      -- lưu trữ cột: ẩn cột và thẻ bên trong nhưng không xóa
  archived_at timestamptz,
  created_at  timestamptz NOT NULL DEFAULT now(),
  updated_at  timestamptz NOT NULL DEFAULT now(),
  CONSTRAINT ck_board_columns_name_len CHECK (char_length(btrim(name)) BETWEEN 1 AND 40),   -- BR-BRD-04
  CONSTRAINT ck_board_columns_soft_delete CHECK (is_deleted = (deleted_at IS NOT NULL)),
  CONSTRAINT ck_board_columns_archive CHECK (is_archived = (archived_at IS NOT NULL))
);
CREATE UNIQUE INDEX ux_board_columns_name ON board_columns (board_id, lower(name)) WHERE NOT is_deleted AND NOT is_archived;
CREATE INDEX ix_board_columns_board_pos ON board_columns (board_id, position) WHERE NOT is_deleted AND NOT is_archived;

CREATE TABLE cards (
  id            uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
  column_id     uuid        NOT NULL REFERENCES board_columns(id) ON DELETE CASCADE,
  title         text        NOT NULL,
  description   text,
  owner_note    text,                                  -- ghi chú chỉ đạo, chỉ Owner sửa (BR-CRD-13)
  position      text        COLLATE "C" NOT NULL,      -- khóa sắp xếp trong cột (BR-CRD-04)
  priority      text        NOT NULL DEFAULT 'medium', -- BR-CRD-03
  start_at      timestamptz,                           -- ngày bắt đầu, UTC (không muộn hơn hạn chót)
  due_at        timestamptz,                           -- hạn chót, UTC (BR-CRD-05)
  completed_at  timestamptz,                           -- lúc thẻ được kéo vào cột IsDone, xóa khi kéo ra (Backend ghi); dùng cho thống kê (FR-ACT-03)
  cover_color   text,                                  -- màu bìa thẻ (10 màu cố định); chỉ chọn màu hoặc ảnh
  cover_attachment_id uuid,                            -- ảnh bìa lấy từ một đính kèm CỦA CHÍNH THẺ NÀY (khóa ngoại kép ở dưới)
  is_archived   boolean     NOT NULL DEFAULT false,    -- lưu trữ: ẩn khỏi board nhưng không xóa, tìm lại được
  archived_at   timestamptz,
  created_by_id uuid        REFERENCES users(id) ON DELETE SET NULL,
  row_version   bigint      NOT NULL DEFAULT 1,
  is_deleted    boolean     NOT NULL DEFAULT false,
  deleted_at    timestamptz,
  created_at    timestamptz NOT NULL DEFAULT now(),
  updated_at    timestamptz NOT NULL DEFAULT now(),
  CONSTRAINT ck_cards_title_len CHECK (char_length(btrim(title)) BETWEEN 1 AND 200),     -- BR-CRD-01
  CONSTRAINT ck_cards_description_len CHECK (description IS NULL OR char_length(description) <= 10000),
  CONSTRAINT ck_cards_owner_note_len CHECK (owner_note IS NULL OR char_length(owner_note) <= 2000),
  CONSTRAINT ck_cards_priority CHECK (priority IN ('low', 'medium', 'high', 'urgent')),
  CONSTRAINT ck_cards_soft_delete CHECK (is_deleted = (deleted_at IS NOT NULL)),
  CONSTRAINT ck_cards_dates CHECK (start_at IS NULL OR due_at IS NULL OR start_at <= due_at),
  CONSTRAINT ck_cards_cover_color CHECK (cover_color IS NULL OR cover_color IN ('red', 'orange', 'yellow', 'green', 'teal', 'blue', 'indigo', 'purple', 'pink', 'gray')),
  CONSTRAINT ck_cards_cover_one CHECK (cover_color IS NULL OR cover_attachment_id IS NULL),
  CONSTRAINT ck_cards_archive CHECK (is_archived = (archived_at IS NOT NULL))
);
CREATE INDEX ix_cards_column_position ON cards (column_id, position) WHERE NOT is_deleted AND NOT is_archived;  -- tải một cột theo thứ tự
CREATE INDEX ix_cards_due_at ON cards (due_at) WHERE NOT is_deleted AND NOT is_archived AND due_at IS NOT NULL;  -- quét thẻ sắp đến hạn
-- Tìm kiếm không phân biệt hoa thường và dấu (BR-SRCH-01)
CREATE INDEX ix_cards_title_search ON cards USING gin (public.f_unaccent(title) public.gin_trgm_ops)
  WHERE NOT is_deleted;
CREATE INDEX ix_cards_description_search ON cards USING gin (public.f_unaccent(coalesce(description, '')) public.gin_trgm_ops)
  WHERE NOT is_deleted;

CREATE TABLE card_assignees (
  card_id        uuid        NOT NULL REFERENCES cards(id) ON DELETE CASCADE,
  user_id        uuid        NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  assigned_by_id uuid        REFERENCES users(id) ON DELETE SET NULL,   -- chỉ Owner được gán (BR-CRD-15)
  assigned_at    timestamptz NOT NULL DEFAULT now(),
  PRIMARY KEY (card_id, user_id)
);
CREATE INDEX ix_card_assignees_user ON card_assignees (user_id);   -- "Việc của tôi"

CREATE TABLE labels (
  id            uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
  workspace_id  uuid        NOT NULL REFERENCES workspaces(id) ON DELETE CASCADE,
  name          text        NOT NULL,
  color         text        NOT NULL,
  created_at    timestamptz NOT NULL DEFAULT now(),
  updated_at    timestamptz NOT NULL DEFAULT now(),
  CONSTRAINT ck_labels_name_len CHECK (char_length(btrim(name)) BETWEEN 1 AND 30),
  -- Bảng 10 màu cố định (BR-CRD-07); client ánh xạ tên màu sang mã màu cụ thể
  CONSTRAINT ck_labels_color CHECK (color IN ('red', 'orange', 'yellow', 'green', 'teal', 'blue', 'indigo', 'purple', 'pink', 'gray'))
);
CREATE UNIQUE INDEX ux_labels_workspace_name ON labels (workspace_id, lower(name));

CREATE TABLE card_labels (
  card_id  uuid NOT NULL REFERENCES cards(id)  ON DELETE CASCADE,
  label_id uuid NOT NULL REFERENCES labels(id) ON DELETE CASCADE,   -- xóa nhãn thì gỡ khỏi mọi thẻ (BR-CRD-07)
  PRIMARY KEY (card_id, label_id)
);
CREATE INDEX ix_card_labels_label ON card_labels (label_id);

-- ----------------------------------------------------------------------------
-- 3b. Checklist, đính kèm, theo dõi và gắn sao (giống Trello)
-- ----------------------------------------------------------------------------
CREATE TABLE checklists (
  id          uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
  card_id     uuid        NOT NULL REFERENCES cards(id) ON DELETE CASCADE,
  title       text        NOT NULL,
  position    text        COLLATE "C" NOT NULL,
  created_at  timestamptz NOT NULL DEFAULT now(),
  updated_at  timestamptz NOT NULL DEFAULT now(),
  CONSTRAINT ck_checklists_title_len CHECK (char_length(btrim(title)) BETWEEN 1 AND 100)
);
CREATE INDEX ix_checklists_card ON checklists (card_id, position);

CREATE TABLE checklist_items (
  id            uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
  checklist_id  uuid        NOT NULL REFERENCES checklists(id) ON DELETE CASCADE,
  content       text        NOT NULL,
  is_checked    boolean     NOT NULL DEFAULT false,
  checked_at    timestamptz,                           -- lúc tick xong; luôn khớp với is_checked
  checked_by_id uuid        REFERENCES users(id) ON DELETE SET NULL,
  position      text        COLLATE "C" NOT NULL,
  created_at    timestamptz NOT NULL DEFAULT now(),
  updated_at    timestamptz NOT NULL DEFAULT now(),
  CONSTRAINT ck_checklist_items_content_len CHECK (char_length(btrim(content)) BETWEEN 1 AND 500),
  CONSTRAINT ck_checklist_items_checked CHECK (is_checked = (checked_at IS NOT NULL))
);
CREATE INDEX ix_checklist_items_list ON checklist_items (checklist_id, position);

CREATE TABLE attachments (
  id             uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
  card_id        uuid        NOT NULL REFERENCES cards(id) ON DELETE CASCADE,
  type           text        NOT NULL,                  -- file (lưu trên Cloudinary) hoặc link (đường dẫn ngoài)
  name           text        NOT NULL,
  url            text        NOT NULL,
  mime_type      text,
  size_bytes     bigint,                                -- chỉ có với file
  public_id      text,                                  -- id trên Cloudinary: Backend phải xóa file bằng id này trước khi xóa hẳn dữ liệu
  uploaded_by_id uuid        REFERENCES users(id) ON DELETE SET NULL,
  created_at     timestamptz NOT NULL DEFAULT now(),
  CONSTRAINT ck_attachments_type CHECK (type IN ('file', 'link')),
  CONSTRAINT ck_attachments_name_len CHECK (char_length(btrim(name)) BETWEEN 1 AND 255),
  CONSTRAINT ck_attachments_url CHECK (url ~* '^https?://'),
  CONSTRAINT ck_attachments_shape CHECK (
       (type = 'file' AND size_bytes IS NOT NULL AND size_bytes > 0 AND public_id IS NOT NULL)
    OR (type = 'link' AND size_bytes IS NULL AND public_id IS NULL)),
  CONSTRAINT ux_attachments_id_card UNIQUE (id, card_id)  -- để thẻ chỉ chọn bìa từ đính kèm của chính nó
);
CREATE INDEX ix_attachments_card ON attachments (card_id, created_at);

-- Ảnh bìa phải là đính kèm của chính thẻ đó. Gỡ đính kèm thì bìa tự trống (cần PostgreSQL 15 trở lên).
ALTER TABLE cards ADD CONSTRAINT fk_cards_cover_attachment
  FOREIGN KEY (cover_attachment_id, id) REFERENCES attachments (id, card_id)
  ON DELETE SET NULL (cover_attachment_id);

CREATE TABLE card_watchers (
  card_id     uuid        NOT NULL REFERENCES cards(id) ON DELETE CASCADE,
  user_id     uuid        NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  created_at  timestamptz NOT NULL DEFAULT now(),
  PRIMARY KEY (card_id, user_id)
);
CREATE INDEX ix_card_watchers_user ON card_watchers (user_id);

CREATE TABLE board_stars (
  user_id     uuid        NOT NULL REFERENCES users(id)  ON DELETE CASCADE,
  board_id    uuid        NOT NULL REFERENCES boards(id) ON DELETE CASCADE,
  starred_at  timestamptz NOT NULL DEFAULT now(),
  PRIMARY KEY (user_id, board_id)
);
CREATE INDEX ix_board_stars_board ON board_stars (board_id);

-- ----------------------------------------------------------------------------
-- 4. Bình luận
-- ----------------------------------------------------------------------------
CREATE TABLE comments (
  id                 uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
  card_id            uuid        NOT NULL REFERENCES cards(id) ON DELETE CASCADE,
  author_id          uuid        REFERENCES users(id) ON DELETE SET NULL,  -- NULL khi tài khoản đã bị xóa (BR-ACC-14)
  author_name_snapshot text,                                               -- tên hiển thị khi tác giả rời workspace hoặc xóa tài khoản (BR-CMT-04)
  content            text        NOT NULL,
  edited_at          timestamptz,                                          -- nhãn "đã chỉnh sửa" (BR-CMT-02)
  row_version        bigint      NOT NULL DEFAULT 1,
  is_deleted         boolean     NOT NULL DEFAULT false,
  deleted_at         timestamptz,
  created_at         timestamptz NOT NULL DEFAULT now(),
  updated_at         timestamptz NOT NULL DEFAULT now(),
  CONSTRAINT ck_comments_content CHECK (char_length(content) BETWEEN 1 AND 2000 AND btrim(content) <> ''),  -- BR-CMT-01
  CONSTRAINT ck_comments_soft_delete CHECK (is_deleted = (deleted_at IS NOT NULL))
);
CREATE INDEX ix_comments_card ON comments (card_id, created_at) WHERE NOT is_deleted;

CREATE TABLE comment_reactions (
  comment_id  uuid        NOT NULL REFERENCES comments(id) ON DELETE CASCADE,
  user_id     uuid        NOT NULL REFERENCES users(id)    ON DELETE CASCADE,
  emoji       text        NOT NULL,
  created_at  timestamptz NOT NULL DEFAULT now(),
  PRIMARY KEY (comment_id, user_id, emoji),            -- mỗi người một lần cho mỗi biểu tượng
  CONSTRAINT ck_comment_reactions_emoji CHECK (char_length(emoji) BETWEEN 1 AND 16)
);
CREATE INDEX ix_comment_reactions_user ON comment_reactions (user_id);

-- ----------------------------------------------------------------------------
-- 5. Chat (kênh chung của workspace và chat 1-1)
-- ----------------------------------------------------------------------------
CREATE TABLE chats (
  id           uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
  type         text        NOT NULL,
  workspace_id uuid        REFERENCES workspaces(id) ON DELETE CASCADE,   -- chỉ loại workspace
  user_a_id    uuid        REFERENCES users(id) ON DELETE SET NULL,       -- chỉ loại direct, luôn user_a_id < user_b_id
  user_b_id    uuid        REFERENCES users(id) ON DELETE SET NULL,
  created_at   timestamptz NOT NULL DEFAULT now(),
  CONSTRAINT ck_chats_type CHECK (type IN ('workspace', 'direct')),
  CONSTRAINT ck_chats_shape CHECK (
       (type = 'workspace' AND workspace_id IS NOT NULL AND user_a_id IS NULL AND user_b_id IS NULL)
    OR (type = 'direct' AND workspace_id IS NULL
        AND (user_a_id IS NULL OR user_b_id IS NULL OR user_a_id < user_b_id))
  )
);
-- Mỗi workspace nhóm đúng một kênh chat (BR-CHT-03); mỗi cặp người dùng một hội thoại direct (BR-CHT-11)
CREATE UNIQUE INDEX ux_chats_workspace ON chats (workspace_id) WHERE type = 'workspace';
CREATE UNIQUE INDEX ux_chats_direct ON chats (user_a_id, user_b_id) WHERE type = 'direct';

-- Personal Workspace không có kênh chat (BR-CHT-03)
CREATE FUNCTION trg_chats_guard() RETURNS trigger
LANGUAGE plpgsql AS $$
BEGIN
  IF NEW.type = 'workspace'
     AND EXISTS (SELECT 1 FROM workspaces WHERE id = NEW.workspace_id AND is_personal) THEN
    RAISE EXCEPTION 'Personal Workspace không có kênh chat' USING ERRCODE = '23514';
  END IF;
  RETURN NEW;
END $$;
CREATE TRIGGER trg_chats_guard
  BEFORE INSERT OR UPDATE ON chats
  FOR EACH ROW EXECUTE FUNCTION trg_chats_guard();

-- Tạo kênh chat tự động cùng workspace nhóm (BR-CHT-03)
CREATE FUNCTION trg_workspaces_create_channel() RETURNS trigger
LANGUAGE plpgsql AS $$
BEGIN
  INSERT INTO chats (type, workspace_id) VALUES ('workspace', NEW.id);
  RETURN NEW;
END $$;
CREATE TRIGGER trg_workspaces_create_channel
  AFTER INSERT ON workspaces
  FOR EACH ROW WHEN (NOT NEW.is_personal)
  EXECUTE FUNCTION trg_workspaces_create_channel();

CREATE TABLE messages (
  id                  uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
  chat_id     uuid        NOT NULL REFERENCES chats(id) ON DELETE CASCADE,
  sender_id           uuid        REFERENCES users(id) ON DELETE SET NULL,
  sender_name_snapshot text,                                               -- giống comments.author_name_snapshot (BR-CHT-05)
  content             text        NOT NULL,
  edited_at           timestamptz,
  row_version         bigint      NOT NULL DEFAULT 1,
  is_deleted          boolean     NOT NULL DEFAULT false,
  deleted_at          timestamptz,
  created_at          timestamptz NOT NULL DEFAULT now(),
  updated_at          timestamptz NOT NULL DEFAULT now(),
  CONSTRAINT ck_messages_content CHECK (char_length(content) BETWEEN 1 AND 2000 AND btrim(content) <> ''),  -- BR-CHT-01
  CONSTRAINT ck_messages_soft_delete CHECK (is_deleted = (deleted_at IS NOT NULL))
);
-- Tải lịch sử theo con trỏ, từ mới đến cũ (BR-CHT-07)
CREATE INDEX ix_messages_chat ON messages (chat_id, created_at DESC, id DESC) WHERE NOT is_deleted;

CREATE TABLE chat_reads (
  chat_id uuid        NOT NULL REFERENCES chats(id) ON DELETE CASCADE,
  user_id         uuid        NOT NULL REFERENCES users(id)         ON DELETE CASCADE,
  last_read_at    timestamptz NOT NULL DEFAULT now(),
  PRIMARY KEY (chat_id, user_id)        -- mốc đọc cuối để đếm tin chưa đọc (BR-CHT-09)
);

-- ----------------------------------------------------------------------------
-- 6. Thông báo và nhật ký hoạt động
-- ----------------------------------------------------------------------------
CREATE TABLE notifications (
  id          uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id     uuid        NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  type        text        NOT NULL,
  card_id     uuid        REFERENCES cards(id)    ON DELETE CASCADE,
  message_id  uuid        REFERENCES messages(id) ON DELETE CASCADE,   -- Mentioned trong chat (BR-NTF-08)
  actor_id    uuid        REFERENCES users(id)    ON DELETE SET NULL,
  due_at      timestamptz,                                              -- hạn chót tại thời điểm tạo DueSoon (BR-NTF-03)
  read_at     timestamptz,
  created_at  timestamptz NOT NULL DEFAULT now(),
  CONSTRAINT ck_notifications_type CHECK (type IN ('card_assigned', 'mentioned', 'due_soon', 'comment_added', 'note_updated', 'card_updated')),
  CONSTRAINT ck_notifications_target CHECK (
       (type = 'mentioned' AND (card_id IS NOT NULL OR message_id IS NOT NULL))
    OR (type <> 'mentioned' AND card_id IS NOT NULL)
  ),
  CONSTRAINT ck_notifications_due CHECK (type <> 'due_soon' OR due_at IS NOT NULL),
  CONSTRAINT ck_notifications_not_self CHECK (actor_id IS NULL OR actor_id <> user_id)   -- BR-NTF-02
);
CREATE INDEX ix_notifications_user_read ON notifications (user_id, read_at);            -- đếm chưa đọc
CREATE INDEX ix_notifications_user_created ON notifications (user_id, created_at DESC);
CREATE INDEX ix_notifications_created ON notifications (created_at);                    -- dọn thông báo quá 90 ngày (BR-NTF-06)
-- Mỗi cặp (thẻ, người nhận) chỉ một DueSoon cho mỗi hạn chót; đổi hạn chót thì được gửi lại (BR-NTF-03)
CREATE UNIQUE INDEX ux_notifications_due_soon ON notifications (user_id, card_id, due_at)
  WHERE type = 'due_soon';
-- Gộp NoteUpdated chưa đọc của cùng thẻ thành một (BR-NTF-09): upsert theo chỉ mục này
CREATE UNIQUE INDEX ux_notifications_note_unread ON notifications (user_id, card_id)
  WHERE type = 'note_updated' AND read_at IS NULL;

CREATE TABLE activity_logs (
  id           uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
  workspace_id uuid        NOT NULL REFERENCES workspaces(id) ON DELETE CASCADE,
  board_id     uuid,                                   -- không có khóa ngoại: nhật ký giữ 12 tháng, độc lập với việc xóa board
  actor_id     uuid        REFERENCES users(id) ON DELETE SET NULL,
  action       text        NOT NULL,
  entity_type  text        NOT NULL,
  entity_id    uuid        NOT NULL,
  metadata     jsonb       NOT NULL DEFAULT '{}'::jsonb,
  occurred_at  timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX ix_activity_workspace ON activity_logs (workspace_id, occurred_at DESC);
CREATE INDEX ix_activity_board ON activity_logs (board_id, occurred_at DESC) WHERE board_id IS NOT NULL;
CREATE INDEX ix_activity_entity ON activity_logs (entity_type, entity_id, occurred_at DESC);

-- Chỉ thêm mới (BR-ACT-01). Ngoại lệ duy nhất: ẩn danh hóa actor_id khi xóa tài khoản (BR-ACC-14).
CREATE FUNCTION trg_activity_logs_append_only() RETURNS trigger
LANGUAGE plpgsql AS $$
BEGIN
  IF NEW.actor_id IS NOT NULL
     OR (NEW.id, NEW.workspace_id, NEW.board_id, NEW.action, NEW.entity_type, NEW.entity_id, NEW.metadata, NEW.occurred_at)
        IS DISTINCT FROM
        (OLD.id, OLD.workspace_id, OLD.board_id, OLD.action, OLD.entity_type, OLD.entity_id, OLD.metadata, OLD.occurred_at)
  THEN
    RAISE EXCEPTION 'activity_logs chỉ được thêm mới, không được sửa (BR-ACT-01)' USING ERRCODE = '23514';
  END IF;
  RETURN NEW;
END $$;
CREATE TRIGGER trg_activity_logs_append_only
  BEFORE UPDATE ON activity_logs
  FOR EACH ROW EXECUTE FUNCTION trg_activity_logs_append_only();

-- ----------------------------------------------------------------------------
-- 7. Trigger updated_at và row_version
-- ----------------------------------------------------------------------------
DO $$
DECLARE t text;
BEGIN
  FOREACH t IN ARRAY ARRAY['users', 'workspaces', 'invitations',
                           'boards', 'board_columns', 'labels', 'checklists', 'checklist_items'] LOOP
    EXECUTE format('CREATE TRIGGER trg_%1$s_updated_at BEFORE UPDATE ON %1$I
                    FOR EACH ROW EXECUTE FUNCTION trg_set_updated_at()', t);
  END LOOP;
  FOREACH t IN ARRAY ARRAY['cards', 'comments', 'messages'] LOOP
    EXECUTE format('CREATE TRIGGER trg_%1$s_row_version BEFORE UPDATE ON %1$I
                    FOR EACH ROW EXECUTE FUNCTION trg_bump_row_version()', t);
  END LOOP;
END $$;

-- ----------------------------------------------------------------------------
-- 8. View tiện lợi
-- ----------------------------------------------------------------------------
-- Thẻ đang hoạt động (chưa xóa, chưa lưu trữ, cột chưa lưu trữ) kèm cột, board, workspace và trạng thái quá hạn / sắp đến hạn.
CREATE VIEW v_cards AS
SELECT c.id,
       c.title,
       c.priority,
       c.due_at,
       c.position,
       c.column_id,
       bc.name     AS column_name,
       bc.is_done  AS column_is_done,
       b.id        AS board_id,
       b.name      AS board_name,
       b.workspace_id,
       -- Quá hạn: có hạn chót đã qua và thẻ không nằm trong cột IsDone (BR-CRD-05)
       (c.due_at IS NOT NULL AND c.due_at < now() AND NOT bc.is_done) AS is_overdue,
       -- Sắp đến hạn: còn dưới 24 giờ (BR-NTF-03)
       (c.due_at IS NOT NULL AND c.due_at >= now() AND c.due_at < now() + interval '24 hours'
        AND NOT bc.is_done) AS is_due_soon
FROM cards c
JOIN board_columns bc ON bc.id = c.column_id AND NOT bc.is_deleted AND NOT bc.is_archived
JOIN boards b         ON b.id = bc.board_id  AND NOT b.is_deleted
WHERE NOT c.is_deleted AND NOT c.is_archived;

-- Tiến độ checklist của thẻ (hiện dạng "3/8" trên thẻ).
CREATE VIEW v_card_checklist_progress AS
SELECT cl.card_id,
       count(ci.id)                                  AS total_items,
       count(ci.id) FILTER (WHERE ci.is_checked)     AS checked_items
FROM checklists cl
LEFT JOIN checklist_items ci ON ci.checklist_id = cl.id
GROUP BY cl.card_id;

-- ----------------------------------------------------------------------------
-- 9. Supabase: không để dữ liệu lộ qua API công khai
-- ----------------------------------------------------------------------------
-- Supabase tự mở schema public qua PostgREST cho vai trò anon/authenticated. Bảng users có password_hash,
-- nên bật Row Level Security (không tạo policy = từ chối hết). Backend kết nối bằng vai trò chủ bảng
-- (postgres) nên không bị ảnh hưởng. Trên PostgreSQL thường (không có vai trò anon) khối này không làm gì.
DO $$
DECLARE t record;
BEGIN
  IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'anon') THEN
    FOR t IN SELECT tablename FROM pg_tables WHERE schemaname = 'public' LOOP
      EXECUTE format('ALTER TABLE public.%I ENABLE ROW LEVEL SECURITY', t.tablename);
    END LOOP;
  END IF;
END $$;

COMMIT;
