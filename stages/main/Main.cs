using Godot;

public partial class Main : Node {
    // 选择要实例化的 Mob 场景，使用 Export 特性导出的变量可以在 Godot 检查器中查看和修改（需要先构建项目）
    [Export]
    public PackedScene MobScene { get; set; }

    [ExportCategory("Mob Velocity")]
    [Export(PropertyHint.Range, "50,150,0.5,suffix:px/s")]
    public float MobMinVelocity { get; set; } = 150.0f;
    [Export(PropertyHint.Range, "150,250,0.5,suffix:px/s")]
    public float MobMaxVelocity { get; set; } = 250.0f;

    // Main 场景树
    private SceneTree _sceneTree;

    // 玩家节点
    private Player _player;
    // 玩家起始位置节点
    private Marker2D _startPosition;
    // 敌人生成位置节点
    private PathFollow2D _mobSpawnLocation;
    // 敌人生成计时器节点：控制敌人的生成频率（0.5s/敌人）
    private Timer _mobTimer;
    // 分数计时器节点：计分（1分/s）
    private Timer _scoreTimer;
    // 开始计时器节点：游戏开始倒计时（延迟 2s），一次性（不自动重启）
    private Timer _startTimer;
    // 信息界面节点
    private HUD _hud;

    // 游戏分数
    private int _score;

    /// <inheritdoc />
    /// <remarks>
    /// 节点就绪时调用（节点和其子节点都进入场景树）
    /// </remarks>
    public override void _Ready() {
        // 获取 Main 场景树
        _sceneTree = GetTree();

        // 获取玩家节点
        _player = GetNode<Player>("Player");
        // 获取玩家起始位置节点
        _startPosition = GetNode<Marker2D>("StartPosition");
        // 获取敌人生成位置节点
        _mobSpawnLocation = GetNode<PathFollow2D>("MobPath/MobSpawnLocation");
        // 获取敌人生成计时器节点
        _mobTimer = GetNode<Timer>("MobTimer");
        // 获取分数计时器节点
        _scoreTimer = GetNode<Timer>("ScoreTimer");
        // 获取开始计时器节点
        _startTimer = GetNode<Timer>("StartTimer");
        // 获取信息界面节点
        _hud = GetNode<HUD>("HUD");
    }

    /// <summary>
    /// 游戏结束
    /// </summary>
    public async void GameOver() {
        // 停止敌人生成计时器
        _mobTimer.Stop();
        // 停止分数计时器
        _scoreTimer.Stop();

        GD.Print($"Game Over: Score {_score}");
        // 显示游戏结束信息
        await _hud.ShowGameOver();
    }

    /// <summary>
    /// 开始游戏：重置分数、玩家、敌人，更新信息界面
    /// </summary>
    public void NewGame() {
        // 重置分数
        _score = 0;
        // 更新分数到信息界面
        _hud.UpdateScore(_score);

        // 重置玩家（设置出生点等）
        _player.ResetPlayer(_startPosition.Position);
        // 删除旧的敌人：调用 mobs 组中每个 Mob 节点的删除函数
        // QueueFree：在当前帧结束时，销毁 Mob 节点（包括子节点）并释放内存
        _sceneTree.CallGroup("mobs", Node.MethodName.QueueFree);

        // 显示准备提示
        _hud.ShowMessage("Get Ready!\n准备开始！");
        // 启动开始计时器（2s 延迟后正式开始）
        _startTimer.Start();

        GD.Print("New Game");
    }

    /// <summary>
    /// 监听开始计时器结束信号
    /// </summary>
    private void OnStartTimerTimeout() {
        // 启动敌人生成和分数计时器（开始生成敌人和计分）
        _mobTimer.Start();
        _scoreTimer.Start();
        // GD.Print("OnStartTimerTimeout: Game started");
    }

    /// <summary>
    /// 监听敌人生成计时器结束信号：在敌人生成位置生成一个敌人，方向和速度随机（有范围）
    /// </summary>
    private void OnMobTimerTimeout() {
        // 实例化一个 Mob 场景为节点（创建一个敌人）
        var mob = MobScene.Instantiate<Mob>();

        // 选择 MobPath（Path2D）路径（Curve2D）上的一个随机位置
        // ProgressRatio 表示沿路径走过的距离，用比例表示，范围 [0.0, 1.0]
        _mobSpawnLocation.ProgressRatio = GD.Randf();
        // 设置敌人的位置为该位置
        mob.Position = _mobSpawnLocation.Position;

        // 设定敌人的基础朝向，与路径方向垂直并向内
        // 因为路径是顺时针绘制的，所以顺时针旋转 π/2（弧度 radian）即 90° 后，敌人朝向就会与路径方向垂直并向内
        float direction = _mobSpawnLocation.Rotation + Mathf.Pi / 2;
        // 给敌人朝向增加随机偏移，范围 [-π/4, π/4] 即 [-45°, 45°]
        direction += (float)GD.RandRange(-Mathf.Pi / 4, Mathf.Pi / 4);
        // 设置敌人的最终朝向：与路径方向垂直并向内 + 左右随机偏移
        mob.Rotation = direction;

        // 创建一个朝右（x 轴正方向）的基础速度向量，速度大小随机，范围 [MobMinVelocity, MobMaxVelocity]
        // Godot 里弧度 0 rad（角度 0°）对应的方向为 x 轴正方向
        var velocity = new Vector2((float)GD.RandRange(MobMinVelocity, MobMaxVelocity), 0);
        // 将基础速度向量旋转到敌人的移动方向，并设置为线速度
        // 注意区分敌人朝向（方向）和速度向量（大小+方向）
        // 如果敌人朝向与移动方向不一致，可能会出现朝向和移动方向不一致的情况（比如敌人朝向左上，但移动方向是右下）
        mob.LinearVelocity = velocity.Rotated(direction);

        // 添加敌人节点到 Main 场景树中
        AddChild(mob);
        // GD.Print("OnMobTimerTimeout: Mob created");
    }

    /// <summary>
    /// 监听分数计时器结束信号：分数加 1
    /// </summary>
    private void OnScoreTimerTimeout() {
        // 1分/s
        _score++;
        // 更新分数到信息界面
        _hud.UpdateScore(_score);

        // GD.Print($"OnScoreTimerTimeout: Score {_score}");
    }
}
