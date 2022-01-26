using System.Threading.Tasks;

namespace OPOS.P1.Lib.Threading
{
    public interface ICustomTask
    {
        int UsableCores { get; }
        int Priority { get; }
        float Progress { get; }

        TaskStatus Status { get; }

        CustomTask Deserialize(string json);
        string Serialize<T>() where T : ICustomTaskState;
    }
}