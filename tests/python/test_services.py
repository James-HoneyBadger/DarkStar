from __future__ import annotations

import logging
from pathlib import Path

import pytest

pytest.importorskip("hb_zayfer")

from hb_zayfer.services import (  # noqa: E402
    AppInfo,
    AppPaths,
    AuditService,
    BackupService,
    ConfigService,
    KeyService,
    WorkspaceSummary,
)


@pytest.fixture(autouse=True)
def _isolated_home(tmp_path: Path, monkeypatch: pytest.MonkeyPatch):
    monkeypatch.setenv("DARKSTAR_HOME", str(tmp_path))


def test_app_info_is_consistent():
    info = AppInfo.current()
    assert info.brand_name == "DarkStar"
    assert info.version
    assert "DarkStar" in info.window_title


def test_app_paths_follow_configured_home(tmp_path: Path):
    paths = AppPaths.current()
    assert paths.home_dir == tmp_path
    assert paths.config_dir == tmp_path


def test_key_service_generates_and_summarizes_keys():
    result = KeyService.generate_key(
        algorithm="ed25519",
        label="service-key",
        passphrase=b"secret-pass",
        user_id="service-key",
    )

    assert result.algorithm == "ed25519"
    assert result.label == "service-key"
    assert result.fingerprint

    summary = WorkspaceSummary.collect()
    assert summary.key_count >= 1


def test_audit_service_exposes_recent_entries():
    KeyService.generate_key(
        algorithm="ed25519",
        label="audit-key",
        passphrase=b"secret-pass",
        user_id="audit-key",
    )
    entries = AuditService.recent_entries(limit=5)
    assert isinstance(entries, list)


def test_backup_service_roundtrip(tmp_path: Path):
    KeyService.generate_key(
        algorithm="ed25519",
        label="backup-key",
        passphrase=b"secret-pass",
        user_id="backup-key",
    )
    KeyService.add_contact("Backup Test")

    backup_path = tmp_path / "service-backup.hbzf"
    manifest = BackupService.create_backup(backup_path, "backup-pass", "svc-backup")
    verified = BackupService.verify_backup(backup_path, "backup-pass")

    assert manifest.label == "svc-backup"
    assert verified.integrity_hash == manifest.integrity_hash


def test_config_service_invalid_json_falls_back_to_defaults(caplog: pytest.LogCaptureFixture):
    config_path = ConfigService.config_path()
    config_path.parent.mkdir(parents=True, exist_ok=True)
    config_path.write_text("{\n", encoding="utf-8")

    with caplog.at_level(logging.WARNING):
        cfg = ConfigService.load()

    assert cfg["cipher"] == "AES-256-GCM"
    assert cfg["kdf"] == "Argon2id"
    assert cfg["argon2_memory_mib"] == 64
    assert cfg["argon2_iterations"] == 3
    assert "Failed to load config" in caplog.text


def test_config_service_set_and_get_value_persists():
    value = ConfigService.set_value("cipher", "ChaCha20-Poly1305")
    assert value == "ChaCha20-Poly1305"

    config_path = ConfigService.config_path()
    persisted = config_path.read_text(encoding="utf-8")
    assert "ChaCha20-Poly1305" in persisted

    reloaded = ConfigService.load()
    assert reloaded["cipher"] == "ChaCha20-Poly1305"
    assert ConfigService.get_value("cipher") == "ChaCha20-Poly1305"
