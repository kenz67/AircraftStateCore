using Microsoft.FlightSimulator.SimConnect;

namespace AircraftStateCore.Services
{
	public interface ISimConnectProxy
	{
		private const uint SIMCONNECT_UNUSED = uint.MaxValue;   //SimConnect.SIMCONNECT_UNUSED

		void AddOnRecvEvent(SimConnect.RecvSimobjectDataEventHandler handler);
		void AddOnRecvException(SimConnect.RecvExceptionEventHandler handler);
		void AddOnRecvOpen(SimConnect.RecvOpenEventHandler handler);
		void AddOnRecvQuit(SimConnect.RecvQuitEventHandler handler);
		void AddToDataDefinition(Enum DefineId, string DatumName, string UnitsName, SIMCONNECT_DATATYPE DatumType, float Epsilon = 0.0f, uint DatumId = SIMCONNECT_UNUSED);
		bool CloseConnection();
		bool ConnectToSim(string Name, nint WindowHandle, uint UserEvent, WaitHandle EventHandle, uint ConfigIndex);
		bool IsConnected();
		void Disconnect();
		void MapClientEventToSimEvent(Enum EventId, string EventName);
		void ReceiveMessage();
		void RegisterDataDefineStruct<T>(Enum dwId);
		void RequestDataOnSimObject(Enum RequestId, Enum DefineId, uint ObjectId, SIMCONNECT_PERIOD Period, SIMCONNECT_DATA_REQUEST_FLAG Flags, uint Origin, uint Interval, uint Limit);
		void SetDataOnSimObject(Enum DefineId, uint ObjectId, SIMCONNECT_DATA_SET_FLAG Flags, object DataSet);
		void TransmitClientEvent(uint ObjectId, Enum EventId, uint Data, Enum GroupId, SIMCONNECT_EVENT_FLAG Flags);
		void TransmitClientEvent_Ex1(uint ObjectId, Enum EventId, Enum GroupId, SIMCONNECT_EVENT_FLAG Flags, uint dwData0, uint dwData1, uint dwData2 = 0, uint dwData3 = 0, uint dwData4 = 0);
	}
}