using Godot;

public partial class Mob : RigidBody2D {
    // 动画精灵节点
    private AnimatedSprite2D _animatedSprite2D;

    /// <inheritdoc />
    /// <remarks>
    /// 节点就绪时调用（节点和其子节点都进入场景树）
    /// </remarks>
    public override void _Ready() {
        // 获取动画精灵节点
        _animatedSprite2D = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        // 获取动画名称：["fly", "swim", "walk"]
        string[] mobTypes = _animatedSprite2D.SpriteFrames.GetAnimationNames();
        // 随机播放动画，随机数范围 [0, mobTypes.Length)
        _animatedSprite2D.Play(mobTypes[GD.Randi() % mobTypes.Length]);
    }

    /// <summary>
    /// 监听 Mob 离开屏幕（由 VisibleOnScreenNotifier2D 节点触发，受其边界矩形影响）
    /// </summary>
    private void OnMobScreenExited() {
        // 在当前帧结束时，销毁 Mob 节点（包括子节点）并释放内存
        QueueFree();
    }
}
