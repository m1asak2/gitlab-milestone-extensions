Step 1: milestone 選択状態を追加する

画面全体で、現在選択中の milestone を保持する仕組みを追加する。

保持する状態

SelectedGroupId

SelectedMemberId

SelectedProjectId

SelectedMilestoneId

最重要なのは SelectedMilestoneId。
Dashboard / Issues / Gantt はすべてこの値を参照する。

初期状態では milestone 未選択を許容する。

Step 2: milestone 選択 UI を追加する

画面上部に選択バーを追加する。

選択順

Group

Member

Project

Milestone

前段の選択に応じて次の候補を絞り込めるようにする。

要件

Group を変えたら Member / Project / Milestone を再選択状態に戻す

Member を変えたら Project / Milestone を再選択状態に戻す

Project を変えたら Milestone を再選択状態に戻す

Milestone を選択した時点で詳細表示対象が確定する

未選択時は各タブにデータを出さない。

Step 3: milestone 選択用 API を追加または整理する

選択 UI を動かすため、候補取得 API を追加または整理する。

必要な候補

Group 一覧

Member 一覧

Project 一覧

Milestone 一覧

実装の細部は既存 API の流用でも新規追加でもよい。
ただし最終的に 1つの milestone を選べること を優先する。

内部識別は title ではなく milestoneId を使うこと。

Step 4: Dashboard を milestoneId ベースに変更する

Dashboard API と画面を、選択中 milestone のみを対象にする。

例

/api/dashboard?milestoneId=123

返す内容

total issues

open issues

closed issues

overdue issues

start

due

estimate

actual

表示ルール

Start = milestone.start_date

Due = milestone.due_date

Estimate = milestone 配下 issue の見積合計

Actual = milestone 配下 issue の実績合計

milestone 未選択時は空表示または案内表示にする。

Step 5: Issues を milestoneId ベースに変更する

Issues API と画面を、選択中 milestone 配下の issue のみ表示するように変更する。

例

/api/issues?milestoneId=123

要件

返却は選択 milestone 配下 issue のみ

一覧には Milestone 列を残す

見積工数 / 実績工数の列は維持する

Milestone 列は同じ値が並ぶが、将来の流用のため残してよい。

Step 6: Gantt を milestoneId ベースに変更する

Gantt API と画面を、選択中 milestone の issue のみ表示するように変更する。

例

/api/gantt?milestoneId=123

要件

返却は選択 milestone 配下 issue のみ

全 issue 横断表示はやめる

今後の前提は「1 milestone に集中した gantt」

milestone 未選択時はデータを出さない。

Step 7: Milestones タブを削除する

タブ構成を見直す。

残すタブ

Dashboard

Issues

Gantt

削除するタブ

Milestones

Milestones 一覧ページや、それに関連する不要な導線も整理する。

Step 8: 集計・未選択状態・識別方法を仕上げる

最後に全体の整合性を調整する。

要件

milestone の識別は title ではなく milestoneId を使う

Estimate = sum(issue.time_estimate)

Actual = sum(issue.total_time_spent)

Overdue Issues = 期限超過かつ未完了の issue 数

milestone 未選択時は Dashboard / Issues / Gantt を呼ばない、または空表示にする

Group / Member / Project の変更時に、下位選択を正しくリセットする

この Step では、画面の一貫性と状態管理の破綻がないことを確認する。