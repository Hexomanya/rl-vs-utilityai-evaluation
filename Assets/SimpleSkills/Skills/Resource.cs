using UnityEngine;

namespace SimpleSkills
{
    public class Resource : ITileContainable
    {
        public Element ContainedElement { get; }
        public int Charges { get; private set; }
        public bool IsObstruction { get => true; }

        public Resource(Element containedElement, int charges)
        {
            this.ContainedElement = containedElement;
            this.Charges = charges;
        }

        public bool ReduceCharges(int reduceByValue = 1)
        {
            this.Charges = Mathf.Max(this.Charges - reduceByValue, 0);
            return this.Charges <= 0;
        }
        public Color TileColor {  get => this.ContainedElement.TileColor; }
        
    }
}
