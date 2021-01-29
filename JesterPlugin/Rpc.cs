using Hazel;
using Reactor;

namespace JesterPlugin
{
    [RegisterCustomRpc]
    public class SetJesterRpc : CustomRpc<JesterPlugin, PlayerControl, SetJesterRpc.Data>
    {
        public SetJesterRpc(JesterPlugin plugin) : base(plugin)
        {
        }

        public readonly struct Data
        {
            public readonly byte Target;

            public Data(byte target)
            {
                Target = target;
            }
        }

        public override RpcLocalHandling LocalHandling => RpcLocalHandling.None;

        public override void Write(MessageWriter writer, Data data)
        {
            writer.Write(data.Target);
        }

        public override Data Read(MessageReader reader)
        {
            return new Data(reader.ReadByte());
        }

        public override void Handle(PlayerControl innerNetObject, Data data)
        {
            System.Console.WriteLine($"{innerNetObject.Data.PlayerId} set #{data.Target} as the jester");
            foreach (var ctrl in PlayerControl.AllPlayerControls)
            {
                if (ctrl.PlayerId == data.Target)
                {
                    JesterPlugin.SetJesterPatch.Jester = ctrl;
                    break;
                }
            }
        }
    }

    [RegisterCustomRpc]
    public class SetJesterWin : CustomRpc<JesterPlugin, PlayerControl, SetJesterWin.Data>
    {
        public SetJesterWin(JesterPlugin plugin) : base(plugin)
        {
        }

        public readonly struct Data
        {
            public readonly string Message;

            public Data(string message)
            {
                Message = message;
            }
        }

        public override RpcLocalHandling LocalHandling => RpcLocalHandling.None;

        public override void Write(MessageWriter writer, Data data)
        {
            writer.Write(data.Message);
        }

        public override Data Read(MessageReader reader)
        {
            return new Data(reader.ReadString());
        }

        public override void Handle(PlayerControl innerNetObject, Data data)
        {
            System.Console.WriteLine($"Jester won!");
            JesterPlugin.PlayerControlWinPatch.HandleWinRpc();
            
        }
    }
}