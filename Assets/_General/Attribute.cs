
namespace _General
{
    [System.Serializable]
    public class Attribute<T>
    {
        public T MaxValue { get; private set; }
        public T Value { get; private set; }

        public Attribute(T maxValue, T startValue)
        {
            this.MaxValue = maxValue;
            this.Value = startValue;
        }

        public void Set(T newValue)
        {
            this.Value = newValue;
        }

        public void Reset()
        {
            this.Value = this.MaxValue;
        }
    }
}
