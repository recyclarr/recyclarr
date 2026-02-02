namespace Recyclarr.Cli.Processors.StateRepair;

internal class StatsAccumulator
{
    public int Adopted { get; private set; }
    public int Corrected { get; private set; }
    public int Removed { get; private set; }
    public int Skipped { get; private set; }
    public int NotInService { get; private set; }
    public int Unchanged { get; private set; }
    public int Preserved { get; private set; }

    public void RecordAdopted() => Adopted++;

    public void RecordCorrected() => Corrected++;

    public void RecordRemoved() => Removed++;

    public void RecordSkipped() => Skipped++;

    public void RecordNotInService() => NotInService++;

    public void RecordUnchanged() => Unchanged++;

    public void RecordPreserved() => Preserved++;

    public StateRepairStats ToStats() =>
        new(Adopted, Corrected, Removed, Skipped, NotInService, Unchanged, Preserved);
}
