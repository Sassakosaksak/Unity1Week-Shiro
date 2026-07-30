# Unity-1Week-Shiro

## OutlineFx の導入メモ

このリポジトリでは、公開時の再配布リスクを避けるため `Assets/Outline` はコミットしない。
Outline を使う場合は、各自で Asset Store から `Outline 2D/3D` を導入する。

導入後、Unity 6 / URP で次の警告が出る場合がある。

```text
The render pass ... does not have an implementation of the RecordRenderGraph method.
```

その場合は次の設定を ON にする。

```text
Edit > Project Settings > Graphics > URP > Compatibility Mode (RenderGraph disabled)
```

設定ファイル上では次の値。

```yaml
m_EnableRenderCompatibilityMode: 1
```

2D Renderer で Depth buffer の `SetRenderTarget` エラーが出る場合は、
`OutlineFxFeature` の `_attachDepth` を OFF にする。

```yaml
_attachDepth: 0
```
