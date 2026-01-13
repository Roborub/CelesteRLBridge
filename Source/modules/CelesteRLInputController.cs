using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.CelesteRLAgentBridge;

public static class CelesteRLInputController
{
    public static BoolNode JumpNode => CelesteRLAgentGlobals.JumpNode;
    public static BoolNode DashNode => CelesteRLAgentGlobals.DashNode;
    public static BoolNode GrabNode => CelesteRLAgentGlobals.GrabNode;


    public static void Setup()
    {
        // Redirect buttons
        Input.Jump = new VirtualButton(JumpNode);
        Input.Dash = new VirtualButton(DashNode);
        Input.Grab = new VirtualButton(GrabNode);

        // Initialize Aim as a fresh joystick 
        // We don't need PadStick because we will set the Value manually
        Input.Aim = new VirtualJoystick(true);
    }


    public static void Reset()
    {
        // Neutral state: No movement, no buttons held
        Input.MoveX.Value = 0;
        Input.MoveY.Value = 0;
        JumpNode.Checked = false;
        DashNode.Checked = false;
        GrabNode.Checked = false;
    }

    public static void Apply(int[] actions)
    {
        if (actions == null || actions.Length < 5)
        {
            Reset();
            return;
        }

        // Map actions to values: 0 = -1, 1 = 1, 2 = 0
        float moveX = (actions[0] == 0) ? -1f : (actions[0] == 1 ? 1f : 0f);
        float moveY = (actions[1] == 0) ? -1f : (actions[1] == 1 ? 1f : 0f);

        // Set movement
        Input.MoveX.Value = (int)moveX;
        Input.MoveY.Value = (int)moveY;

        // Set Aim (This enables vertical/diagonal dashes)
        // We set the Vector2 directly
        Input.Aim.Value = new Vector2(moveX, moveY);

        // Button states
        JumpNode.Checked = (actions[2] == 1);
        DashNode.Checked = (actions[3] == 1);
        GrabNode.Checked = (actions[4] == 1);
    }
}
