namespace FatAntelope.Writers
{
    public abstract class BaseDiffWriter
    {
        public abstract void WriteDiff(XTree tree, string file);
    }
}
