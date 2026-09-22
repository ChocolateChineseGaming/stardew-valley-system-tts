# Stardew Valley macOS 系统普通话配音

这是一个独立的 SMAPI Mod，只使用 macOS 自带的 `say` 和 `afplay`，不包含 Qwen、模型服务或第三方 TTS 依赖。

支持手动朗读 NPC 对白、任务详情、电视节目和信件。打开内容后按 `F8` 开始朗读；再次按 `F8` 会从头重读。按 `F10` 打开设置页，可调整系统合成语速、播放速度、音量和朗读内容。

## 工作方式

1. 优先播放 `audio/<key>.wav` 中已有的语音库文件。
2. 未命中时调用 macOS `/usr/bin/say` 生成 WAV，并缓存到 Mod 的 `cache` 目录。
3. 使用 `/usr/bin/afplay` 播放，切换页面或关闭菜单时停止旧语音。

整个运行流程不访问网络。系统声音必须已在 macOS 中安装，可在终端执行 `say -v '?'` 查看声音名称。

## 构建

需要 .NET 6 SDK、Stardew Valley 和 SMAPI：

```sh
dotnet build src/SystemVoice -c Release
python3 tools/package.py
```

若游戏不在 Steam 默认目录：

```sh
dotnet build src/SystemVoice -c Release \
  -p:GamePath="/完整路径/Stardew Valley/Contents/MacOS"
```

打包结果为 `dist/StardewSystemVoice-0.1.0.zip`。将其中的 `StardewSystemVoice` 文件夹放进游戏的 `Contents/MacOS/Mods`。

不要与原来的 `StardewMandarin.MacVoice` 同时启用，否则两个 Mod 都会响应 F8。这个项目使用独立 ID：`StardewMandarin.SystemVoice`。

## 配置

首次运行会生成 `config.json`：

```json
{
  "Enabled": true,
  "ReadQuestText": true,
  "ReadLetters": true,
  "ReadNonNpcDialogue": true,
  "FallbackVoice": "Tingting (中文（中国大陆）)",
  "SpeechRate": 175,
  "NpcVoices": {
    "Penny": "Tingting (中文（中国大陆）)",
    "Abigail": "Flo (中文（中国大陆）)"
  },
  "Volume": 1.0,
  "PlaybackRate": 0.85,
  "NpcVolume": {},
  "ReplayKey": "F8",
  "ConfigKey": "F10",
  "CaptureMissingDialogue": true
}
```

`FallbackVoice` 是未单独配置角色时使用的声音。`NpcVoices` 可按 NPC 内部英文名指定声音；声音名必须与 `say -v '?'` 的输出一致。声音或 `SpeechRate` 改变后会使用新的缓存键，不会误用旧缓存。

## 批量生成语音库

游戏中遇到的缺失文本会记录到 `missing-dialogue.jsonl`。退出游戏后可运行：

```sh
python3 tools/voice_library.py generate \
  "/完整路径/Mods/StardewSystemVoice/missing-dialogue.jsonl" \
  --output "/完整路径/Mods/StardewSystemVoice/audio" \
  --voices voice-source/voices.json
```

## 验证

```sh
python3 -m unittest discover -s tests -v
dotnet run --project tests/VoiceKeyChecks
dotnet run --project tests/VoiceKeyChecks -- --speech-test
```

最后一项会真实调用系统 TTS，但播放音量为零。游戏内检查步骤见 `docs/verification.md`。
