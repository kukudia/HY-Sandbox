# 维修占用提示

自然Player窗口重复出现基地维修被阻挡，但旧提示仅有“The deployment bay is occupied.”。现在维修/部署失败显示建筑、舰船、残骸或场景物体类别，名称及整数世界坐标，提示清场后重试。仅在请求被拒绝时构造提示，不增加每帧普查；同源模块仍按单位身份忽略。未增加自动清除、穿模或收费行为。

Unity Editor Siege Play：20项大型基地与21项玩法闭环通过，包含明确命名的基地缺件/舰船泊位阻挡物，确认拒绝、金币不变、具体阻挡名称及重试提示。原始报告siege-refit-obstruction-base.json/play.json。Player视觉提示与友舰/残骸分支待后续验证，不把Editor检查写成Player验证。
