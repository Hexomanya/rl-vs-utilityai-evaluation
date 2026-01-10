using UnityEngine;

namespace SimpleSkills
{
    [System.Serializable]
    public abstract class Element
    {
        public string Name { get; }
        public Color TileColor { get; }
        public int MapIndex { get; }

        protected Element(string name, Color tileColor, int mapIndex)
        {
            this.Name = name;
            this.TileColor = tileColor;
            this.MapIndex = mapIndex;
        }
    }

    [System.Serializable]
    public class FireElement : Element
    {
        public FireElement() : base("Fire", new Color(0.8f,0.33f,0f), ObservationConfig.ElementIndexFire){}
    }
    
    [System.Serializable]
    public class WaterElement : Element
    {
        public WaterElement() : base("Water", new Color(0.28f,0.24f,0.55f), ObservationConfig.ElementIndexWater){}
    }
    
    [System.Serializable]
    public class EarthElement : Element
    {
        public EarthElement() : base("Earth", new Color(0.63f,0.32f,0.17f), ObservationConfig.ElementIndexEarth){}
    }
    
    [System.Serializable]
    public class AirElement : Element
    {
        public AirElement() : base("Air", new Color(0.75f,0.75f,0.75f), ObservationConfig.ElementIndexAir){}
    }
}
