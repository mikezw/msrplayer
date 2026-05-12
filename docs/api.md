# Monster Siren API 文档

基于浏览器 Network 抓包整理，时间：2026-05

## 1. 基础信息

**Base URL**
```
https://monster-siren.hypergryph.com/api
```

## 2. 通用响应结构

```json
{
  "code": 0,
  "msg": "OK",
  "data": {}
}
```

| 字段 | 类型 | 说明 |
|------|------|------|
| code | int | 0 表示成功 |
| msg | string | 消息 |
| data | object | 实际数据 |

## 3. 鉴权与限制

| 项 | 情况 |
|----|------|
| 登录 | 不需要 |
| Token | 不需要 |
| Cookie | 不需要 |
| 签名 | 不需要 |
| DRM | 无 |
| 限流 | 未发现明显限制 |
| CORS | API 有限制 |

## 4. API 列表

### 4.1 获取全部歌曲

```
GET /songs
```

完整 URL:
```
https://monster-siren.hypergryph.com/api/songs
```

返回示例:
```json
{
  "code": 0,
  "data": {
    "list": [
      {
        "cid": "4941",
        "name": "Operation Blade",
        "albumCid": "1001",
        "artists": ["塞壬唱片-MSR"]
      }
    ]
  }
}
```

| 字段 | 类型 | 说明 |
|------|------|------|
| cid | string | 歌曲ID |
| name | string | 歌曲名称 |
| albumCid | string | 所属专辑ID |
| artists | string[] | 艺术家 |

### 4.2 获取单曲详情

```
GET /song/{cid}
```

示例:
```
https://monster-siren.hypergryph.com/api/song/4941
```

返回示例:
```json
{
  "code": 0,
  "data": {
    "cid": "4941",
    "name": "Operation Blade",
    "sourceUrl": "https://res01.hycdn.cn/...wav",
    "lyricUrl": "https://monster-siren.hypergryph.com/...lrc",
    "mvUrl": "",
    "coverUrl": "https://monster-siren.hypergryph.com/...jpg",
    "artists": ["塞壬唱片-MSR"],
    "albums": [
      {
        "cid": "1001",
        "name": "Contingency Contract"
      }
    ]
  }
}
```

| 字段 | 类型 | 说明 |
|------|------|------|
| cid | string | 歌曲ID |
| name | string | 歌名 |
| sourceUrl | string | 音频直链（WAV格式） |
| lyricUrl | string | 歌词文件（LRC格式） |
| coverUrl | string | 封面 |
| mvUrl | string | MV地址（可能为空） |
| artists | array | 艺术家 |
| albums | array | 所属专辑 |

### 4.3 获取专辑列表

```
GET /albums
```

完整 URL:
```
https://monster-siren.hypergryph.com/api/albums
```

返回示例:
```json
{
  "code": 0,
  "data": {
    "list": [
      {
        "cid": "1001",
        "name": "Contingency Contract",
        "coverUrl": "...jpg"
      }
    ]
  }
}
```

| 字段 | 类型 | 说明 |
|------|------|------|
| cid | string | 专辑ID |
| name | string | 专辑名 |
| coverUrl | string | 封面 |

### 4.4 获取专辑详情

```
GET /album/{albumCid}
```

示例:
```
https://monster-siren.hypergryph.com/api/album/1001
```

返回示例:
```json
{
  "code": 0,
  "data": {
    "cid": "1001",
    "name": "Contingency Contract",
    "coverUrl": "...jpg",
    "songs": [
      {
        "cid": "4941",
        "name": "Operation Blade"
      }
    ]
  }
}
```

## 5. 音频资源

当前公开 API 实测为 WAV 格式：

| 特性 | 情况 |
|------|------|
| 无损 | 是 |
| HTTP直链 | 是 |
| 可直接播放 | 是 |
| 可直接下载 | 是 |