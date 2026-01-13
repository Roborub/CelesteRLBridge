using Monocle;

namespace Celeste.Mod.CelesteRLAgentBridge;

public class BoolNode : VirtualButton.Node
{
    public bool Checked;
    private bool _lastChecked;

    public override bool Check => Checked;
    public override bool Pressed => Checked && !_lastChecked;
    public override bool Released => !Checked && _lastChecked;

    public override void Update()
    {
        _lastChecked = Checked;
    }
}

public static class CelesteRLAgentGlobals
{
    // Constants
    public const int Port = 5555;
    public const int GridSize = 15;
    public const int TileSize = 8;
    public const int StaticFeatureCount = 9;
    public const int CategoryCount = 8;
    public const int TotalFeatureCount = StaticFeatureCount + (GridSize * GridSize * CategoryCount);

    public static int latestActionIndex = 0;

    public static bool inputInitialized = false;

    public static BoolNode JumpNode = new BoolNode();
    public static BoolNode DashNode = new BoolNode();
    public static BoolNode GrabNode = new BoolNode();

    public static VirtualButton RLJump;
    public static VirtualButton RLDash;
    public static VirtualButton RLGrab;

    public static CelesteRLNetworkServer _server = new CelesteRLNetworkServer();

    public static string LastDetectedTile = "None";

    public const int ObservationSize = 1024 * 5;
    public const float MaxRunSpeed = 280.0f;
    public const float MaxLaunchSpeed = 240.0f;
    public const float MaxStamina = 110.0f;

    public static string[] DebugGrid = new string[GridSize];

    public static int[] PlayerOhv = { 1, 0, 0, 0, 0, 0, 0, 0 };
    public static int[] SolidOhv = { 0, 1, 0, 0, 0, 0, 0, 0};
    public static int[] AirOhv = { 0, 0, 1, 0, 0, 0, 0, 0};
    public static int[] PlatformOhv = { 0, 0, 0, 1, 0, 0, 0, 0 };
    public static int[] HazardOhv = { 0, 0, 0, 0, 1, 0, 0, 0};
    public static int[] StrawberryOhv = { 0, 0, 0, 0, 0, 1, 0, 0 };
    public static int[] BoundaryOhv = { 0, 0, 0, 0, 0, 0, 1, 0 };
    public static int[] MiscEntityOhv = { 0, 0, 0, 0, 0, 0, 0, 1};

    public static string PlayerAscii = "M ";
    public static string MiscEntityAscii = "* ";
    public static string HazardAscii = "X ";
    public static string SolidAscii = "[]";
    public static string AirAscii = "  ";
    public static string BoundaryAscii = "# ";
    public static string StrawberryAscii = "@ ";
    public static string PlatformAscii = "_ ";

}


