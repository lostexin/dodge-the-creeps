using Godot;

public partial class Player : Area2D {
    // 击中信号（自定义信号）：玩家与敌人碰撞时触发
    [Signal]
    public delegate void HitEventHandler();
    // 玩家移动速度（像素/秒），使用 Export 特性导出的变量可以在 Godot 检查器中查看和修改（需要先构建项目）
    [Export]
    public int Speed { get; set; } = 400;

    // 游戏窗口大小
    public Vector2 ScreenSize;
    // 动画精灵节点
    private AnimatedSprite2D _animatedSprite2D;
    // 玩家碰撞形状节点
    private CollisionShape2D _collisionShape2D;
    // 玩家碰撞形状
    private CapsuleShape2D _capsuleShape2D;
    // 玩家碰撞形状的半径
    private Vector2 _capsuleShapeRadius;

    /// <inheritdoc />
    /// <remarks>
    /// 节点就绪时调用（节点和其子节点都进入场景树）
    /// </remarks>
    public override void _Ready() {
        // 游戏开始时隐藏玩家
        Hide();
        // 获取游戏窗口大小
        ScreenSize = GetViewportRect().Size;
        // 获取动画精灵节点
        _animatedSprite2D = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        // 获取玩家碰撞形状节点
        _collisionShape2D = GetNode<CollisionShape2D>("CollisionShape2D");
        // 获取玩家碰撞形状
        _capsuleShape2D = _collisionShape2D.Shape as CapsuleShape2D;
        // 获取玩家碰撞形状的半径：短边为胶囊的半径，长边为胶囊总高度的一半
        _capsuleShapeRadius = new Vector2(_capsuleShape2D.Radius, _capsuleShape2D.Height * 0.5f);
    }

    /// <inheritdoc />
    /// <remarks>
    /// 每帧调用
    /// </remarks>
    /// <param name="delta">
    /// <para>帧之间的时间间隔，单位秒</para>
    /// <para>帧率高时，每秒帧数多，delta 变小；帧率低时，每秒帧数少，delta 变大</para>
    /// <para>使用 delta 来保证游戏按时间计算，而不是按帧数计算，<b>使得游戏在不同帧率下表现一致</b>，避免在低帧率时变慢，在高帧率时变快</para>
    /// </param>
    public override void _Process(double delta) {
        // 玩家速度向量，默认零向量（不动）
        var velocity = Vector2.Zero;
        // 根据自定义输入（项目 - 项目设置 - 输入映射）
        if (Input.IsActionPressed("move_up")) {
            velocity.Y -= 1;
        }
        if (Input.IsActionPressed("move_down")) {
            velocity.Y += 1;
        }
        if (Input.IsActionPressed("move_left")) {
            velocity.X -= 1;
        }
        if (Input.IsActionPressed("move_right")) {
            velocity.X += 1;
        }

        // 选择动画：比较两轴绝对值，优先显示移动幅度更大的方向的动画
        // 当两轴相等时（如斜向移动），优先显示垂直动画
        if (Mathf.Abs(velocity.Y) >= Mathf.Abs(velocity.X) && velocity.Y != 0) {
            // 上下动画
            _animatedSprite2D.Animation = "up";
            // 垂直翻转视方向而定（向下为正，向上为负，默认向上）
            _animatedSprite2D.FlipV = velocity.Y > 0;
            // GD.Print($"velocity.Y: {velocity.Y}");
        } else if (velocity.X != 0) {
            // 左右动画
            _animatedSprite2D.Animation = "walk";
            // 水平翻转视方向而定（向右为正，向左为负，默认向右）
            _animatedSprite2D.FlipH = velocity.X < 0;
            // GD.Print($"velocity.X: {velocity.X}");
        }

        // 贴边斜向移动优化：若玩家已在边界，将朝向边界的方向分量置零
        // 避免速度向量归一化后，与朝向边界方向垂直的分量变为 1/√2（长度），导致移动变慢（速度变为 Speed/√2）
        // 将朝向边界的方向分量置零，另一分量归一化后为 1（长度），同只向一个方向移动时一致
        if (velocity.X > 0 && Position.X >= ScreenSize.X - _capsuleShapeRadius.X) {
            velocity.X = 0; // 已在右边界，取消向右分量
        } else if (velocity.X < 0 && Position.X <= _capsuleShapeRadius.X) {
            velocity.X = 0; // 已在左边界，取消向左分量
        }
        if (velocity.Y > 0 && Position.Y >= ScreenSize.Y - _capsuleShapeRadius.Y) {
            velocity.Y = 0; // 已在下边界，取消向下分量
        } else if (velocity.Y < 0 && Position.Y <= _capsuleShapeRadius.Y) {
            velocity.Y = 0; // 已在上边界，取消向上分量
        }

        // 因为归一化的过程中需要除以向量的长度，所以无法对长度为 0 的向量进行归一化（C# 会报错，GDScript 不会报错）
        if (velocity.Length() > 0) {
            // 斜向移动速度比水平和垂直移动速度快，所以要对速度向量进行归一化（单位化），使其长度为 1 而方向不变
            // 假设同时按住"右"和"下"，则生成的 velocity 向量为 (1, 1)，导致斜向移动速度更快
            velocity = velocity.Normalized() * Speed;
            // 播放动画
            _animatedSprite2D.Play();
        } else {
            _animatedSprite2D.Stop();
        }

        // 移动玩家：速度向量乘以 delta，以保证移动不被帧率变化所影响
        Position += velocity * (float)delta;
        Position = new Vector2(
            // 防止玩家离开游戏窗口：计入玩家碰撞形状的半径，以免玩家半边身体离开窗口
            x: Mathf.Clamp(Position.X, _capsuleShapeRadius.X, ScreenSize.X - _capsuleShapeRadius.X),
            y: Mathf.Clamp(Position.Y, _capsuleShapeRadius.Y, ScreenSize.Y - _capsuleShapeRadius.Y)
        );
    }

    /// <summary>
    /// 开始游戏
    /// </summary>
    public void Start(Vector2 position) {
        // 游戏开始时重置玩家：重置位置、显示玩家、启用玩家碰撞检测
        Position = position;
        Show();
        _collisionShape2D.Disabled = false;
    }

    /// <summary>
    /// 监听 body 进入该区域信号（玩家与敌人碰撞信号）
    /// </summary>
    /// <param name="body"></param>
    private void OnBodyEntered(Node2D body) {
        // 玩家被击中后隐藏
        Hide();
        // 触发击中信号
        EmitSignal(SignalName.Hit);
        // 禁用玩家碰撞检测，防止触发多次击中信号
        // SetDeferred：在当前帧结束时，设置指定属性的值
        // 如果在引擎的碰撞处理过程中禁用区域的碰撞形状可能会导致错误，
        // 使用 SetDeferred() 告诉 Godot 等待可以安全地禁用形状时再这样做
        _collisionShape2D.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
    }
}
