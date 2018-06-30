namespace TinfluxWeatherStation
{
    public interface ISensor
    {
        string SensorTyp();
        string SensorUnit();
        string SensorUnitName();
        
    }
}