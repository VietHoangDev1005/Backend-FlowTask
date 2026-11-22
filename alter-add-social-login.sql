BEGIN;
ALTER TABLE users ALTER COLUMN password_hash DROP NOT NULL;
ALTER TABLE users ADD COLUMN provider text, ADD COLUMN provider_user_id text;
ALTER TABLE users ADD CONSTRAINT ck_users_provider CHECK ((provider IS NULL AND provider_user_id IS NULL)
  OR (provider IN ('google', 'facebook') AND provider_user_id IS NOT NULL AND btrim(provider_user_id) <> ''));
ALTER TABLE users ADD CONSTRAINT ck_users_login_method CHECK (password_hash IS NOT NULL OR provider IS NOT NULL);
CREATE UNIQUE INDEX ux_users_provider ON users (provider, provider_user_id) WHERE provider IS NOT NULL;
COMMIT;
