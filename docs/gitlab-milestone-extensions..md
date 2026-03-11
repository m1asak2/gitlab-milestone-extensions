GitLab の Issues / Milestones を可視化する機能を拡張する。

現在は **Issue ベースの簡易 Gantt** のみだが、
 以下の機能を追加する。

------

# 追加機能

## ISSUES

Issues 一覧に **見積工数 / 実績工数** を追加する。

GitLab Issue API の `time_stats` を利用する。

追加項目

- 見積工数
- 実績工数

表示優先順位

```
human_time_estimate
human_total_time_spent
```

存在しない場合は秒から変換する。

------

## MILESTONES

### 1. グループマイルストーンを表示

現在は

```
GET /projects/:id/milestones
```

のみ使用している。

以下を追加する

```
GET /groups/:id/milestones
```

結果は **統合して表示**する。

Milestone には以下を追加表示する。

- Scope (Project / Group)

------

### 2. 開始日 / 終了日

Milestone API の

```
start_date
due_date
```

を表示する。

表示列

```
Start
Due
```

------

### 3. 見積工数 / 実績工数

Milestone 自体には工数が存在しないため、
 Issue を元に **集計する**

集計方法

```
milestone_id 単位
```

集計内容

```
estimate = Σ issue.time_estimate
spent = Σ issue.total_time_spent
```

Milestone 表示列

```
Estimate
Actual
```

------

# GANTT

## 1. マイルストーン別表示

現在は Issue 単位の Gantt のみ。

以下を追加する。

```
Milestone フィルタ
```

UI

```
[ Milestone ▼ ]
```

選択時

```
/api/gantt?milestone=<title>
```

を呼び出す。

------

## 2. フィルタ

Gantt API に以下を追加する。

Query

```
milestone
milestoneId
```

例

```
/api/gantt
/api/gantt?milestone=Release-1.0
/api/gantt?milestoneId=123
```

------

# API変更

## Issue DTO

追加

```
TimeEstimateSeconds
TotalTimeSpentSeconds
HumanTimeEstimate
HumanTotalTimeSpent
```

取得元

```
issue.time_stats
```

------

## Milestone DTO

追加

```
Scope
StartDate
DueDate
TimeEstimateSeconds
TotalTimeSpentSeconds
```

------

## Gantt DTO

追加

```
MilestoneId
MilestoneTitle
TimeEstimateSeconds
TotalTimeSpentSeconds
```

------

# API仕様

## /api/issues

追加項目

```
estimate
spent
```

------

## /api/milestones

取得対象

```
project milestones
group milestones
```

返却は統合リスト。

------

## /api/gantt

Query

```
milestone
milestoneId
```

フィルタ条件として使用する。

------

# UI変更

## Issues

列追加

```
Estimate
Actual
```

------

## Milestones

列追加

```
Scope
Start
Due
Estimate
Actual
```

------

## Gantt

追加 UI

```
Milestone Filter
```

------

# 実装順序

1. Issue 工数取得
2. Issue UI 列追加
3. Group Milestone API 追加
4. Milestone UI 拡張
5. Milestone 工数集計
6. Gantt milestone フィルタ