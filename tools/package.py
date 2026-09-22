#!/usr/bin/env python3
"""Package the system-TTS-only SMAPI mod."""

from pathlib import Path
import json
import zipfile


root = Path(__file__).resolve().parents[1]
mod = root / "src" / "SystemVoice"
dll = mod / "bin" / "Release" / "net6.0" / "SystemVoice.dll"
if not dll.is_file():
    raise SystemExit("请先执行 dotnet build src/SystemVoice -c Release。")

version = json.loads((mod / "manifest.json").read_text(encoding="utf-8"))["Version"]
output = root / "dist" / f"StardewSystemVoice-{version}.zip"
output.parent.mkdir(exist_ok=True)
with zipfile.ZipFile(output, "w", zipfile.ZIP_DEFLATED) as archive:
    for file in (dll, mod / "manifest.json", root / "README.md", root / "docs" / "verification.md"):
        archive.write(file, "StardewSystemVoice/" + file.name)
    for file in (root / "tools" / "voice_library.py", root / "voice-source" / "voices.json"):
        archive.write(file, "StardewSystemVoice/" + file.relative_to(root).as_posix())
    audio = mod / "audio"
    if audio.is_dir():
        for wav in sorted(audio.glob("*.wav")):
            archive.write(wav, "StardewSystemVoice/audio/" + wav.name)
print(output)
