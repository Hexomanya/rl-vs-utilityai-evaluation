using System;
using Unity.MLAgents.Sensors;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SimpleSkills
{
    public enum SensorType
    {
        Visual,
        Vector,
    }
    
    public class GridSensorComponent : SensorComponent
    {
        [SerializeField] private SensorType _sensorType;
        
        public GridObservator GetObservationVisualSensor { get; private set; }

        public override ISensor[] CreateSensors()
        {
            this.GetObservationVisualSensor = _sensorType switch {
                SensorType.Visual => new GridObservationVisualSensor("WorldGrid" + Random.Range(0f, 10f),
                    ObservationConfig.ChannelCount,
                    ObservationConfig.GridSize,
                    ObservationConfig.GridSize),
                SensorType.Vector => new GridObservationVectorSensor("WorldGrid" + Random.Range(0f, 10f),
                    ObservationConfig.ChannelCount,
                    ObservationConfig.GridSize,
                    ObservationConfig.GridSize),
                _ => throw new ArgumentOutOfRangeException()
            };

            return new ISensor[] { this.GetObservationVisualSensor };
        }
    }
}
