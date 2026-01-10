using Unity.MLAgents.Sensors;
using UnityEngine;

namespace SimpleSkills
{
    public class GridObservationVisualSensor : GridObservator
    {
        public GridObservationVisualSensor(string name, int channelCount, int height, int width) : base(name, channelCount, height, width) { }

        protected override ObservationSpec CreateObservationSpec() { return ObservationSpec.Visual(_channelCount, _height,_width); }
    }
}
