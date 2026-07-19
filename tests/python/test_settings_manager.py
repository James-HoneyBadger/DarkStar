from __future__ import annotations

import json
import logging
from pathlib import Path

import pytest

pytest.importorskip("hb_zayfer")

from hb_zayfer.gui.settings_manager import CryptoConfig, SettingsManager  # noqa: E402
from hb_zayfer.services import KeyService  # noqa: E402


@pytest.fixture(autouse=True)
def _reset_crypto_config_singleton() -> None:
    CryptoConfig._instance = None
    yield
    CryptoConfig._instance = None


def test_kdf_settings_invalid_numbers_fallback_to_defaults(
    tmp_path: Path,
    monkeypatch: pytest.MonkeyPatch,
    caplog: pytest.LogCaptureFixture,
):
    config_path = tmp_path / "config.json"
    config_path.write_text(
        json.dumps(
            {
                "kdf": "scrypt",
                "scrypt_log_n": "not-a-number",
                "scrypt_r": None,
                "scrypt_p": {},
            }
        ),
        encoding="utf-8",
    )
    monkeypatch.setattr(CryptoConfig, "path", staticmethod(lambda: config_path))

    with caplog.at_level(logging.WARNING):
        params = CryptoConfig.instance().kdf_settings()

    assert params == {
        "kdf": "scrypt",
        "kdf_log_n": 15,
        "kdf_r": 8,
        "kdf_p": 1,
    }
    assert "Invalid integer value" in caplog.text


def test_kdf_settings_out_of_range_values_are_clamped(
    tmp_path: Path,
    monkeypatch: pytest.MonkeyPatch,
):
    config_path = tmp_path / "config.json"
    config_path.write_text(
        json.dumps(
            {
                "kdf": "argon2id",
                "argon2_memory_mib": 1,
                "argon2_iterations": 9999,
            }
        ),
        encoding="utf-8",
    )
    monkeypatch.setattr(CryptoConfig, "path", staticmethod(lambda: config_path))

    params = CryptoConfig.instance().kdf_settings()
    assert params["kdf"] == "argon2id"
    assert params["kdf_memory_kib"] == 16 * 1024
    assert params["kdf_iterations"] == 100


def test_settings_manager_save_is_atomic_when_replace_fails(
    tmp_path: Path,
    monkeypatch: pytest.MonkeyPatch,
):
    manager = SettingsManager(config_dir=tmp_path)
    manager.set("theme", "dark")
    manager.save()
    original = (tmp_path / "gui_settings.json").read_text(encoding="utf-8")

    manager.set("theme", "light")
    tmp_file = tmp_path / "gui_settings.tmp"
    real_replace = Path.replace

    def _failing_replace(self: Path, target: Path):
        if self == tmp_file:
            raise OSError("simulated replace failure")
        return real_replace(self, target)

    monkeypatch.setattr(Path, "replace", _failing_replace)
    manager.save()

    current = (tmp_path / "gui_settings.json").read_text(encoding="utf-8")
    assert current == original


def test_settings_manager_set_replaces_non_dict_intermediate(tmp_path: Path):
    manager = SettingsManager(config_dir=tmp_path)
    manager.settings = {"a": 1}

    manager.set("a.b", 2)

    assert manager.settings["a"] == {"b": 2}
    assert manager.get("a.b") == 2


def test_key_service_rejects_unexpected_algorithm_alias():
    with pytest.raises(ValueError, match="Unknown algorithm"):
        KeyService._normalize_algorithm("pgpgpg")
