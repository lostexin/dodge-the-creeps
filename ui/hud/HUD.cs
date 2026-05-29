using Godot;
using System.Threading.Tasks;

public partial class HUD : CanvasLayer {
    // 游戏开始信号（自定义信号）：游戏开始时触发
    [Signal]
    public delegate void StartGameEventHandler();

    // HUD 场景树
    private SceneTree _sceneTree;

    // 分数标签节点
    private Label _scoreLabel;
    // 消息节点
    private Label _message;
    // 开始按钮节点
    private Button _startButton;
    // 消息计时器节点：决定消息的显示时间（默认 2s）
    private Timer _messageTimer;

    // 默认消息（游戏标题）
    private string _defaultMessage = "Dodge the Creeps!\n躲避敌人！";

    /// <inheritdoc />
    /// <remarks>
    /// 节点就绪时调用（节点和其子节点都进入场景树）
    /// </remarks>
    public override void _Ready() {
        // 获取 HUD 场景树
        _sceneTree = GetTree();

        // 获取分数标签节点
        _scoreLabel = GetNode<Label>("ScoreLabel");
        // 获取消息节点
        _message = GetNode<Label>("Message");
        // 获取开始按钮节点
        _startButton = GetNode<Button>("StartButton");
        // 获取消息计时器节点
        _messageTimer = GetNode<Timer>("MessageTimer");

        // 隐藏分数标签
        _scoreLabel.Hide();
    }

    /// <summary>
    /// 更新分数
    /// </summary>
    /// <param name="score">分数</param>
    public void UpdateScore(int score) {
        _scoreLabel.Text = $"Score 分数\n{score}";
    }

    /// <summary>
    /// 显示游戏结束消息，然后返回到游戏开始界面（显示游戏标题和开始按钮）
    /// </summary>
    public async Task ShowGameOver() {
        // 显示游戏结束消息
        ShowMessage("Game Over\n游戏结束");

        // 等待消息计时器结束后显示默认消息（2s 后游戏结束消息变为游戏标题）
        // ToSignal 返回一个可等待 await 的对象 SignalAwaiter（实现了 C# 可等待模式 awaitable pattern），
        // 也就是说 C# 的 await 不要求对象必须是 Task
        await ToSignal(_messageTimer, Timer.SignalName.Timeout);
        ShowMessage(_defaultMessage, false);

        // 等待一次性计时器结束后显示重试按钮（0s 后显示重试按钮）
        // CreateTimer：创建一个 SceneTreeTimer（由场景树管理的一次性计时器）
        await ToSignal(_sceneTree.CreateTimer(0), SceneTreeTimer.SignalName.Timeout);
        _startButton.Text = "Retry\n重试";
        _startButton.Show();
    }

    /// <summary>
    /// 显示消息
    /// </summary>
    /// <param name="text">消息内容</param>
    /// <param name="enableTimer">是否启用计时器</param>
    public void ShowMessage(string text, bool enableTimer = true) {
        _message.Text = text;
        _message.Show();
        if (enableTimer) {
            _messageTimer.Start();
        }
    }

    /// <summary>
    /// 监听开始按钮按下信号：按钮按下后显示分数标签和隐藏开始按钮，并触发游戏开始信号
    /// </summary>
    private void OnStartButtonPressed() {
        // 显示分数标签
        _scoreLabel.Show();
        // 隐藏开始按钮
        _startButton.Hide();
        // 触发游戏开始信号，触发后执行 Main.NewGame()（在 Godot Main 场景的 HUD 节点的信号面板中连接）
        EmitSignal(SignalName.StartGame);
    }

    /// <summary>
    /// 监听消息计时器结束信号：倒计时结束后隐藏消息（2s 后隐藏消息）
    /// </summary>
    private void OnMessageTimerTimeout() {
        _message.Hide();
    }
}
