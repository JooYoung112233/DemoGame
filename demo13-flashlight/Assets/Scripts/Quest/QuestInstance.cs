using System.Collections.Generic;

public class QuestInstance
{
    public QuestData data;
    public QuestState state;
    public Dictionary<int, int> progress = new Dictionary<int, int>();
    public float acceptedTime;

    public QuestInstance(QuestData data, float time)
    {
        this.data = data;
        this.state = QuestState.Active;
        this.acceptedTime = time;
        for (int i = 0; i < data.objectives.Length; i++)
            progress[i] = 0;
    }

    public bool IsObjectiveComplete(int index)
    {
        if (index < 0 || index >= data.objectives.Length) return false;
        return progress.ContainsKey(index) && progress[index] >= data.objectives[index].requiredCount;
    }

    public bool AreAllObjectivesComplete()
    {
        for (int i = 0; i < data.objectives.Length; i++)
        {
            if (!IsObjectiveComplete(i)) return false;
        }
        return true;
    }
}
