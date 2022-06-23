namespace TOMICZ.Grid
{
    [System.Serializable]
    public struct Node
    {
        public int x;
        public int y;

        public Node(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
    }
}