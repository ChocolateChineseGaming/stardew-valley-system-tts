import importlib.util
import json
from pathlib import Path
import tempfile
import unittest
import wave


spec = importlib.util.spec_from_file_location(
    "voice_library", Path(__file__).parents[1] / "tools/voice_library.py")
voice_library = importlib.util.module_from_spec(spec)
spec.loader.exec_module(voice_library)


class VoiceLibraryTests(unittest.TestCase):
    def test_key_normalization_and_speaker_separation(self):
        self.assertNotEqual(
            voice_library.voice_key("Penny", "你好。"),
            voice_library.voice_key("Abigail", "你好。"),
        )
        self.assertEqual(
            voice_library.voice_key("Penny", "  你好\n世界。 "),
            voice_library.voice_key("Penny", "你好 世界。"),
        )

    def test_input_is_deduplicated_and_untrusted_key_is_recomputed(self):
        with tempfile.TemporaryDirectory() as tmp:
            source = Path(tmp) / "rows.jsonl"
            row = json.dumps({"npc": "Penny", "text": "你好。", "key": "../../bad"})
            source.write_text(row + "\n" + row + "\n", encoding="utf-8")
            rows = list(voice_library.read_rows(source))
            self.assertEqual(len(rows), 1)
            self.assertRegex(rows[0][1], r"^[a-f0-9]{64}$")

    def test_wav_validation_rejects_empty_audio(self):
        with tempfile.TemporaryDirectory() as tmp:
            output = Path(tmp) / "test.wav"
            with wave.open(str(output), "wb") as wav:
                wav.setparams((1, 2, 22050, 0, "NONE", "not compressed"))
                wav.writeframes(b"")
            with self.assertRaises(ValueError):
                voice_library.validate_wav(output)


if __name__ == "__main__":
    unittest.main()
