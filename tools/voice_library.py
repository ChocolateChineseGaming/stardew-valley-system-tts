#!/usr/bin/env python3
"""Offline Mandarin WAV library builder. No third-party Python dependencies."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import subprocess
import tempfile
import unicodedata
import wave


def normalize(text):
    return re.sub(r"[ \t\r\n\f\v]+", " ", unicodedata.normalize("NFC", text)).strip(" ")


def voice_key(npc, text):
    return hashlib.sha256((npc + "\n" + normalize(text)).encode("utf-8")).hexdigest()


def read_rows(path):
    seen = set()
    with open(path, encoding="utf-8") as source:
        for number, line in enumerate(source, 1):
            if not line.strip():
                continue
            row = json.loads(line)
            if not all(isinstance(row.get(k), str) and row[k].strip() for k in ("npc", "text")):
                raise ValueError(f"第 {number} 行必须包含非空 npc 和 text 字符串")
            key = voice_key(row["npc"], row["text"])
            if key not in seen:
                seen.add(key)
                yield row, key


def validate_wav(path):
    with wave.open(str(path), "rb") as wav:
        if (wav.getcomptype() != "NONE" or wav.getsampwidth() != 2
                or wav.getnchannels() not in (1, 2) or wav.getnframes() == 0):
            raise ValueError(f"{path}: 需要非空 16-bit PCM 单/双声道 WAV")


def generate(row, key, output, voices):
    profile = voices.get(row["npc"], voices.get("default"))
    if not isinstance(profile, dict) or not isinstance(profile.get("voice"), str):
        raise ValueError(f"未配置 {row['npc']} 的 macOS voice")
    rate = profile.get("rate", 180)
    if not isinstance(rate, int) or not 80 <= rate <= 350:
        raise ValueError("rate 必须是 80–350 的整数")
    # Arguments go directly to the executable: dialogue is never shell code.
    with tempfile.TemporaryDirectory(prefix="mandarin-", dir=output) as work:
        text_path = Path(work) / "dialogue.txt"
        text_path.write_text(normalize(row["text"]), encoding="utf-8")
        wav_path = Path(work) / "voice.wav"
        subprocess.run(["/usr/bin/say", "-v", profile["voice"], "-r", str(rate),
                        "-f", str(text_path), "-o", str(wav_path),
                        "--file-format=WAVE", "--data-format=LEI16@22050"], check=True)
        validate_wav(wav_path)
        os.replace(wav_path, output / (key + ".wav"))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("command", choices=["generate", "check"])
    parser.add_argument("dialogues", type=Path, help="游戏采集或人工整理的 JSONL")
    parser.add_argument("--output", type=Path, required=True, help="Mod 的 audio 目录")
    parser.add_argument("--voices", type=Path, default=Path(__file__).resolve().parents[1] / "voice-source/voices.json")
    args = parser.parse_args()
    rows = list(read_rows(args.dialogues))
    voices = json.loads(args.voices.read_text(encoding="utf-8")) if args.command == "generate" else {}
    if args.command == "generate":
        args.output.mkdir(parents=True, exist_ok=True)
    missing = 0
    for row, key in rows:
        target = args.output / (key + ".wav")
        if not target.exists() and args.command == "generate":
            generate(row, key, args.output, voices)
        if target.exists():
            validate_wav(target)
        else:
            missing += 1
            print(f"缺失: {row['npc']} {key}")
    print(f"共 {len(rows)} 条，已有 {len(rows) - missing} 条，缺失 {missing} 条。")
    return 1 if missing else 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except (ValueError, OSError, subprocess.CalledProcessError, wave.Error, EOFError) as exc:
        raise SystemExit(f"失败：{exc}")
