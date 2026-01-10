using Unity.MLAgents.Sensors;

namespace SimpleSkills
{
    public class GridObservationVectorSensor : GridObservator
    {
        public GridObservationVectorSensor(string name, int channelCount,int height, int width) : base(name, channelCount, height, width) { }

        protected override ObservationSpec CreateObservationSpec()
        {
            return ObservationSpec.Vector(_channelCount * _height * _width);
        }
        
    }
}
