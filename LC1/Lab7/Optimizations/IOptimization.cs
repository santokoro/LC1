namespace LC1.Lab7.Optimizations
{
    public interface IOptimization
    {
        string Name { get; }
        string Description { get; }
        IrProgram Apply(IrProgram input);
    }
}
