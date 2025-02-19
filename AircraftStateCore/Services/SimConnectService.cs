using AircraftStateCore.DAL.Repositories.Interfaces;
using AircraftStateCore.enums;
using AircraftStateCore.Helpers;
using AircraftStateCore.Models;
using AircraftStateCore.Services.Interfaces;
using Microsoft.FlightSimulator.SimConnect;

namespace AircraftStateCore.Services;
public class SimConnectService : ISimConnectService
{
	public PlaneDataStruct SimData { get; set; } = new PlaneDataStruct();
	public MasterData MasterData { get; set; } = new MasterData();
	public event Func<Task> OnChangeAsync;
	public event Func<Task> OnMessageUpdate;
	public string DisplayMessage { get; set; }

	private IntPtr WindowHandle { get; }

	private const int WM_USER_SIMCONNECT = 0x0402;

	//private SimConnect sim;
	private readonly Settings _settings;
	private bool sentToSim = false;
	public bool masterBatteryOn = false;
	private readonly IPlaneDataRepo _planeDataRepo;
	private readonly ISimConnectProxy _proxy;

	public SimConnectService(ISimConnectProxy SimProxy, ISettingsData settings, IPlaneDataRepo planeDataRepo, IDbInit dbInit)
	{
		dbInit.Init();
		_proxy = SimProxy;
		_settings = settings.ReadSettings().Result;
		_planeDataRepo = planeDataRepo;
		var w = MessagePumpWindow.GetWindow();
		WindowHandle = w.Hwnd;
		w.WndProcHandle += W_WndProcHandle;

		//will i have problem with 2024?  https://devsupport.flightsimulator.com/t/2020-2024-compatible-microsoft-flightsimulator-simconnect-dll/11844/2
		_proxy.ConnectToSim("Managed Data Request", WindowHandle, WM_USER_SIMCONNECT, null, 0);
		SetupSimConnect();
	}

	private IntPtr W_WndProcHandle(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
	{
		try
		{
			if (msg == WM_USER_SIMCONNECT)
				ReceiveSimConnectMessage();
		}
		catch
		{
			Disconnect();
		}

		return IntPtr.Zero;
	}

	public void Disconnect()
	{
		_proxy.Disconnect();
	}

	private void ReceiveSimConnectMessage()
	{
		_proxy.ReceiveMessage();
	}

	public bool Connected()
	{
		return _proxy.IsConnected();
	}

	//todo, real test?, just instantiate svc and this will be called.
	private void SetupSimConnect()
	{
		try
		{
			_proxy.AddOnRecvOpen(new SimConnect.RecvOpenEventHandler(SimConnect_OnRecvOpen));
			_proxy.AddOnRecvQuit(new SimConnect.RecvQuitEventHandler(SimConnect_OnRecvQuit));
			_proxy.AddOnRecvEvent(new SimConnect.RecvSimobjectDataEventHandler(SimConnect_OnRecvSimobjectData));
			_proxy.AddOnRecvException(new SimConnect.RecvExceptionEventHandler(SimConnect_OnRecvException));

			SetupReadDataDefinitions();
			SetupEnvironmentDataDefinitions();
			SetupWritableDataDefinitions();
			RegisterDataStructures();

			SetupDataRequests();

			MapEvents();

		}
		catch /* (COMException ex) */
		{
		}
	}

	private void SetupReadDataDefinitions()
	{
		//Data being captured
		_proxy.AddToDataDefinition(DATA_DEFINITIONS.SimPlaneDataStructure, "PLANE LATITUDE", "degrees", SIMCONNECT_DATATYPE.FLOAT64);
		_proxy.AddToDataDefinition(DATA_DEFINITIONS.SimPlaneDataStructure, "PLANE LONGITUDE", "degrees", SIMCONNECT_DATATYPE.FLOAT64);
		_proxy.AddToDataDefinition(DATA_DEFINITIONS.SimPlaneDataStructure, "PLANE ALTITUDE", "feet", SIMCONNECT_DATATYPE.INT32);
		_proxy.AddToDataDefinition(DATA_DEFINITIONS.SimPlaneDataStructure, "PLANE HEADING DEGREES MAGNETIC", "degrees", SIMCONNECT_DATATYPE.FLOAT64);
		_proxy.AddToDataDefinition(DATA_DEFINITIONS.SimPlaneDataStructure, "PLANE PITCH DEGREES", "degrees", SIMCONNECT_DATATYPE.FLOAT64);
		_proxy.AddToDataDefinition(DATA_DEFINITIONS.SimPlaneDataStructure, "COM ACTIVE FREQUENCY:1", "Mhz", SIMCONNECT_DATATYPE.FLOAT64);
		_proxy.AddToDataDefinition(DATA_DEFINITIONS.SimPlaneDataStructure, "COM STANDBY FREQUENCY:1", "Mhz", SIMCONNECT_DATATYPE.FLOAT64);
		_proxy.AddToDataDefinition(DATA_DEFINITIONS.SimPlaneDataStructure, "COM ACTIVE FREQUENCY:2", "Mhz", SIMCONNECT_DATATYPE.FLOAT64);
		_proxy.AddToDataDefinition(DATA_DEFINITIONS.SimPlaneDataStructure, "COM STANDBY FREQUENCY:2", "Mhz", SIMCONNECT_DATATYPE.FLOAT64);
		_proxy.AddToDataDefinition(DATA_DEFINITIONS.SimPlaneDataStructure, "NAV ACTIVE FREQUENCY:1", "Mhz", SIMCONNECT_DATATYPE.FLOAT64);
		_proxy.AddToDataDefinition(DATA_DEFINITIONS.SimPlaneDataStructure, "NAV STANDBY FREQUENCY:1", "Mhz", SIMCONNECT_DATATYPE.FLOAT64);
		_proxy.AddToDataDefinition(DATA_DEFINITIONS.SimPlaneDataStructure, "NAV ACTIVE FREQUENCY:2", "Mhz", SIMCONNECT_DATATYPE.FLOAT64);
		_proxy.AddToDataDefinition(DATA_DEFINITIONS.SimPlaneDataStructure, "NAV STANDBY FREQUENCY:2", "Mhz", SIMCONNECT_DATATYPE.FLOAT64);
		_proxy.AddToDataDefinition(DATA_DEFINITIONS.SimPlaneDataStructure, "ADF ACTIVE FREQUENCY:1", "Mhz", SIMCONNECT_DATATYPE.FLOAT64);
		_proxy.AddToDataDefinition(DATA_DEFINITIONS.SimPlaneDataStructure, "ADF STANDBY FREQUENCY:1", "Mhz", SIMCONNECT_DATATYPE.FLOAT64);
		_proxy.AddToDataDefinition(DATA_DEFINITIONS.SimPlaneDataStructure, "NAV OBS:1", "degrees", SIMCONNECT_DATATYPE.FLOAT64);
		_proxy.AddToDataDefinition(DATA_DEFINITIONS.SimPlaneDataStructure, "NAV OBS:2", "degrees", SIMCONNECT_DATATYPE.FLOAT64);
		_proxy.AddToDataDefinition(DATA_DEFINITIONS.SimPlaneDataStructure, "ADF CARD", "degrees", SIMCONNECT_DATATYPE.FLOAT64);
		_proxy.AddToDataDefinition(DATA_DEFINITIONS.SimPlaneDataStructure, "FUEL TANK CENTER QUANTITY", "gallons", SIMCONNECT_DATATYPE.FLOAT64);
		_proxy.AddToDataDefinition(DATA_DEFINITIONS.SimPlaneDataStructure, "FUEL TANK CENTER2 QUANTITY", "gallons", SIMCONNECT_DATATYPE.FLOAT64);
		_proxy.AddToDataDefinition(DATA_DEFINITIONS.SimPlaneDataStructure, "FUEL TANK CENTER3 QUANTITY", "gallons", SIMCONNECT_DATATYPE.FLOAT64);
		_proxy.AddToDataDefinition(DATA_DEFINITIONS.SimPlaneDataStructure, "FUEL TANK EXTERNAL1 QUANTITY", "gallons", SIMCONNECT_DATATYPE.FLOAT64);
		_proxy.AddToDataDefinition(DATA_DEFINITIONS.SimPlaneDataStructure, "FUEL TANK EXTERNAL2 QUANTITY", "gallons", SIMCONNECT_DATATYPE.FLOAT64);
		_proxy.AddToDataDefinition(DATA_DEFINITIONS.SimPlaneDataStructure, "FUEL TANK LEFT AUX QUANTITY", "gallons", SIMCONNECT_DATATYPE.FLOAT64);
		_proxy.AddToDataDefinition(DATA_DEFINITIONS.SimPlaneDataStructure, "FUEL TANK LEFT MAIN QUANTITY", "gallons", SIMCONNECT_DATATYPE.FLOAT64);
		_proxy.AddToDataDefinition(DATA_DEFINITIONS.SimPlaneDataStructure, "FUEL TANK LEFT TIP QUANTITY", "gallons", SIMCONNECT_DATATYPE.FLOAT64);
		_proxy.AddToDataDefinition(DATA_DEFINITIONS.SimPlaneDataStructure, "FUEL TANK RIGHT AUX QUANTITY", "gallons", SIMCONNECT_DATATYPE.FLOAT64);
		_proxy.AddToDataDefinition(DATA_DEFINITIONS.SimPlaneDataStructure, "FUEL TANK RIGHT MAIN QUANTITY", "gallons", SIMCONNECT_DATATYPE.FLOAT64);
		_proxy.AddToDataDefinition(DATA_DEFINITIONS.SimPlaneDataStructure, "FUEL TANK RIGHT TIP QUANTITY", "gallons", SIMCONNECT_DATATYPE.FLOAT64);
		_proxy.AddToDataDefinition(DATA_DEFINITIONS.SimPlaneDataStructure, "FUEL TANK SELECTOR:1", "enum", SIMCONNECT_DATATYPE.INT32);
		_proxy.AddToDataDefinition(DATA_DEFINITIONS.SimPlaneDataStructure, "BRAKE PARKING INDICATOR", "bool", SIMCONNECT_DATATYPE.INT32);
		_proxy.AddToDataDefinition(DATA_DEFINITIONS.SimPlaneDataStructure, "KOHLSMAN SETTING HG", "inHg", SIMCONNECT_DATATYPE.FLOAT64);
		_proxy.AddToDataDefinition(DATA_DEFINITIONS.SimPlaneDataStructure, "AUTOPILOT HEADING LOCK DIR", "degrees", SIMCONNECT_DATATYPE.FLOAT64);
		_proxy.AddToDataDefinition(DATA_DEFINITIONS.SimPlaneDataStructure, "FLAPS HANDLE INDEX", "number", SIMCONNECT_DATATYPE.INT32);
		_proxy.AddToDataDefinition(DATA_DEFINITIONS.SimPlaneDataStructure, "ELEVATOR TRIM POSITION", "radians", SIMCONNECT_DATATYPE.FLOAT64);
		_proxy.AddToDataDefinition(DATA_DEFINITIONS.SimPlaneDataStructure, "RUDDER TRIM PCT", "percent", SIMCONNECT_DATATYPE.FLOAT64);
		_proxy.AddToDataDefinition(DATA_DEFINITIONS.SimPlaneDataStructure, "AILERON TRIM PCT", "percent", SIMCONNECT_DATATYPE.FLOAT64);
		_proxy.AddToDataDefinition(DATA_DEFINITIONS.SimPlaneDataStructure, "GYRO DRIFT ERROR", "radians", SIMCONNECT_DATATYPE.FLOAT64);
		_proxy.AddToDataDefinition(DATA_DEFINITIONS.SimPlaneDataStructure, "HEADING INDICATOR", "degrees", SIMCONNECT_DATATYPE.FLOAT64);

		//key on these to trigger db save. all 3 must be turned on together, then all 3 off together
		_proxy.AddToDataDefinition(DATA_DEFINITIONS.SimPlaneDataStructure, "ELECTRICAL MASTER BATTERY", "bool", SIMCONNECT_DATATYPE.INT32);
		_proxy.AddToDataDefinition(DATA_DEFINITIONS.SimPlaneDataStructure, "GENERAL ENG MASTER ALTERNATOR", "bool", SIMCONNECT_DATATYPE.INT32);
		_proxy.AddToDataDefinition(DATA_DEFINITIONS.SimPlaneDataStructure, "AVIONICS MASTER SWITCH", "bool", SIMCONNECT_DATATYPE.INT32);
		_proxy.AddToDataDefinition(DATA_DEFINITIONS.SimPlaneDataStructure, "ELECTRICAL BATTERY VOLTAGE", "volts", SIMCONNECT_DATATYPE.FLOAT64);

		//Lights
		_proxy.AddToDataDefinition(DATA_DEFINITIONS.SimPlaneDataStructure, "LIGHT NAV ON", "bool", SIMCONNECT_DATATYPE.INT32);
		_proxy.AddToDataDefinition(DATA_DEFINITIONS.SimPlaneDataStructure, "LIGHT BEACON ON", "bool", SIMCONNECT_DATATYPE.INT32);
		_proxy.AddToDataDefinition(DATA_DEFINITIONS.SimPlaneDataStructure, "LIGHT LANDING ON", "bool", SIMCONNECT_DATATYPE.INT32);
		_proxy.AddToDataDefinition(DATA_DEFINITIONS.SimPlaneDataStructure, "LIGHT TAXI ON", "bool", SIMCONNECT_DATATYPE.INT32);
		_proxy.AddToDataDefinition(DATA_DEFINITIONS.SimPlaneDataStructure, "LIGHT STROBE ON", "bool", SIMCONNECT_DATATYPE.INT32);
		_proxy.AddToDataDefinition(DATA_DEFINITIONS.SimPlaneDataStructure, "LIGHT PANEL ON", "bool", SIMCONNECT_DATATYPE.INT32);
		_proxy.AddToDataDefinition(DATA_DEFINITIONS.SimPlaneDataStructure, "LIGHT RECOGNITION ON", "bool", SIMCONNECT_DATATYPE.INT32);
		_proxy.AddToDataDefinition(DATA_DEFINITIONS.SimPlaneDataStructure, "LIGHT WING ON", "bool", SIMCONNECT_DATATYPE.INT32);
		_proxy.AddToDataDefinition(DATA_DEFINITIONS.SimPlaneDataStructure, "LIGHT CABIN ON", "bool", SIMCONNECT_DATATYPE.INT32);
		_proxy.AddToDataDefinition(DATA_DEFINITIONS.SimPlaneDataStructure, "LIGHT LOGO ON", "bool", SIMCONNECT_DATATYPE.INT32);
		_proxy.AddToDataDefinition(DATA_DEFINITIONS.SimPlaneDataStructure, "LIGHT GLARESHIELD POWER SETTING", "percent", SIMCONNECT_DATATYPE.FLOAT64);
		_proxy.AddToDataDefinition(DATA_DEFINITIONS.SimPlaneDataStructure, "LIGHT CABIN POWER SETTING", "percent", SIMCONNECT_DATATYPE.FLOAT64);
		_proxy.AddToDataDefinition(DATA_DEFINITIONS.SimPlaneDataStructure, "LIGHT PEDESTRAL POWER SETTING", "percent", SIMCONNECT_DATATYPE.FLOAT64);
		_proxy.AddToDataDefinition(DATA_DEFINITIONS.SimPlaneDataStructure, "LIGHT POTENTIOMETER:3", "percent over 100", SIMCONNECT_DATATYPE.FLOAT64);

		_proxy.AddToDataDefinition(DATA_DEFINITIONS.SimPlaneDataStructure, "TRANSPONDER CODE:1", "Bco16", SIMCONNECT_DATATYPE.INT32);
	}

	private void SetupEnvironmentDataDefinitions()
	{
		//Capture the name of the plane
		_proxy.AddToDataDefinition(DATA_DEFINITIONS.SimEnvironmentDataStructure, "Title", null, SIMCONNECT_DATATYPE.STRING256);
	}

	private void SetupDataRequests()
	{
		//Request data from sim
		_proxy.RequestDataOnSimObject(DATA_REQUESTS_TYPES.DataRequest, DATA_DEFINITIONS.SimPlaneDataStructure, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.SECOND, SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT, 0, 0, 0);
		_proxy.RequestDataOnSimObject(DATA_REQUESTS_TYPES.SimEnvironmentReq, DATA_DEFINITIONS.SimEnvironmentDataStructure, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.SECOND, SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT, 0, 0, 0);

	}

	private void SetupWritableDataDefinitions()
	{
		//////////////////
		//Settable Data
		//////////////////

		//Fuel
		_proxy.AddToDataDefinition(DATA_DEFINITIONS.SimPlaneFuelData, "FUEL TANK CENTER QUANTITY", "gallons", SIMCONNECT_DATATYPE.FLOAT64);
		_proxy.AddToDataDefinition(DATA_DEFINITIONS.SimPlaneFuelData, "FUEL TANK CENTER2 QUANTITY", "gallons", SIMCONNECT_DATATYPE.FLOAT64);
		_proxy.AddToDataDefinition(DATA_DEFINITIONS.SimPlaneFuelData, "FUEL TANK CENTER3 QUANTITY", "gallons", SIMCONNECT_DATATYPE.FLOAT64);
		_proxy.AddToDataDefinition(DATA_DEFINITIONS.SimPlaneFuelData, "FUEL TANK EXTERNAL1 QUANTITY", "gallons", SIMCONNECT_DATATYPE.FLOAT64);
		_proxy.AddToDataDefinition(DATA_DEFINITIONS.SimPlaneFuelData, "FUEL TANK EXTERNAL2 QUANTITY", "gallons", SIMCONNECT_DATATYPE.FLOAT64);
		_proxy.AddToDataDefinition(DATA_DEFINITIONS.SimPlaneFuelData, "FUEL TANK LEFT AUX QUANTITY", "gallons", SIMCONNECT_DATATYPE.FLOAT64);
		_proxy.AddToDataDefinition(DATA_DEFINITIONS.SimPlaneFuelData, "FUEL TANK LEFT MAIN QUANTITY", "gallons", SIMCONNECT_DATATYPE.FLOAT64);
		_proxy.AddToDataDefinition(DATA_DEFINITIONS.SimPlaneFuelData, "FUEL TANK LEFT TIP QUANTITY", "gallons", SIMCONNECT_DATATYPE.FLOAT64);
		_proxy.AddToDataDefinition(DATA_DEFINITIONS.SimPlaneFuelData, "FUEL TANK RIGHT AUX QUANTITY", "gallons", SIMCONNECT_DATATYPE.FLOAT64);
		_proxy.AddToDataDefinition(DATA_DEFINITIONS.SimPlaneFuelData, "FUEL TANK RIGHT MAIN QUANTITY", "gallons", SIMCONNECT_DATATYPE.FLOAT64);
		_proxy.AddToDataDefinition(DATA_DEFINITIONS.SimPlaneFuelData, "FUEL TANK RIGHT TIP QUANTITY", "gallons", SIMCONNECT_DATATYPE.FLOAT64);

		//Position
		_proxy.AddToDataDefinition(DATA_DEFINITIONS.SimPlaneLocationData, "PLANE LATITUDE", "degrees", SIMCONNECT_DATATYPE.FLOAT64);
		_proxy.AddToDataDefinition(DATA_DEFINITIONS.SimPlaneLocationData, "PLANE LONGITUDE", "degrees", SIMCONNECT_DATATYPE.FLOAT64);
		_proxy.AddToDataDefinition(DATA_DEFINITIONS.SimPlaneLocationData, "PLANE ALTITUDE", "feet", SIMCONNECT_DATATYPE.INT32);
		_proxy.AddToDataDefinition(DATA_DEFINITIONS.SimPlaneLocationData, "PLANE HEADING DEGREES MAGNETIC", "degrees", SIMCONNECT_DATATYPE.FLOAT64);
		_proxy.AddToDataDefinition(DATA_DEFINITIONS.SimPlaneLocationData, "PLANE PITCH DEGREES", "degrees", SIMCONNECT_DATATYPE.FLOAT64);

		//Trim
		_proxy.AddToDataDefinition(DATA_DEFINITIONS.SimTrimData, "ELEVATOR TRIM POSITION", "radians", SIMCONNECT_DATATYPE.FLOAT64);
		_proxy.AddToDataDefinition(DATA_DEFINITIONS.SimTrimData, "RUDDER TRIM PCT", "percent", SIMCONNECT_DATATYPE.FLOAT64);
		_proxy.AddToDataDefinition(DATA_DEFINITIONS.SimTrimData, "AILERON TRIM PCT", "percent", SIMCONNECT_DATATYPE.FLOAT64);

		//Power
		_proxy.AddToDataDefinition(DATA_DEFINITIONS.SimPowerData, "ELECTRICAL BATTERY VOLTAGE", "volts", SIMCONNECT_DATATYPE.FLOAT64);
	}

	private void RegisterDataStructures()
	{
		//Register the data structures being used
		_proxy.RegisterDataDefineStruct<PlaneDataStruct>(DATA_DEFINITIONS.SimPlaneDataStructure);
		_proxy.RegisterDataDefineStruct<MasterData>(DATA_DEFINITIONS.SimEnvironmentDataStructure);
		_proxy.RegisterDataDefineStruct<MasterData>(DATA_DEFINITIONS.SimPlaneFuelData);
		_proxy.RegisterDataDefineStruct<MasterData>(DATA_DEFINITIONS.SimPlaneLocationData);
		_proxy.RegisterDataDefineStruct<MasterData>(DATA_DEFINITIONS.SimFlapsData);
		_proxy.RegisterDataDefineStruct<MasterData>(DATA_DEFINITIONS.SimLightData);
		_proxy.RegisterDataDefineStruct<MasterData>(DATA_DEFINITIONS.SimPowerData);
	}

	private void MapEvents()
	{
		//Map Events
		_proxy.MapClientEventToSimEvent(EVENT_IDS.COM_RADIO_SET_HZ, "COM_RADIO_SET_HZ");
		_proxy.MapClientEventToSimEvent(EVENT_IDS.COM_STBY_RADIO_SET_HZ, "COM_STBY_RADIO_SET_HZ");
		_proxy.MapClientEventToSimEvent(EVENT_IDS.COM2_RADIO_SET_HZ, "COM2_RADIO_SET_HZ");
		_proxy.MapClientEventToSimEvent(EVENT_IDS.COM2_STBY_RADIO_SET_HZ, "COM2_STBY_RADIO_SET_HZ");
		_proxy.MapClientEventToSimEvent(EVENT_IDS.NAV1_RADIO_SET_HZ, "NAV1_RADIO_SET_HZ");
		_proxy.MapClientEventToSimEvent(EVENT_IDS.NAV1_STBY_SET_HZ, "NAV1_STBY_SET_HZ");
		_proxy.MapClientEventToSimEvent(EVENT_IDS.NAV2_RADIO_SET_HZ, "NAV2_RADIO_SET_HZ");
		_proxy.MapClientEventToSimEvent(EVENT_IDS.NAV2_STBY_SET_HZ, "NAV2_STBY_SET_HZ");
		_proxy.MapClientEventToSimEvent(EVENT_IDS.ADF1_RADIO_SWAP, "ADF1_RADIO_SWAP");
		_proxy.MapClientEventToSimEvent(EVENT_IDS.ADF_SET, "ADF_COMPLETE_SET");
		_proxy.MapClientEventToSimEvent(EVENT_IDS.ADF_STBY_SET, "ADF_STBY_SET");
		_proxy.MapClientEventToSimEvent(EVENT_IDS.OBS1, "VOR1_SET");
		_proxy.MapClientEventToSimEvent(EVENT_IDS.OBS2, "VOR2_SET");
		_proxy.MapClientEventToSimEvent(EVENT_IDS.ADF_CARD_SET, "ADF_CARD_SET");
		_proxy.MapClientEventToSimEvent(EVENT_IDS.FUEL_SELECTOR_SET, "FUEL_SELECTOR_SET");
		_proxy.MapClientEventToSimEvent(EVENT_IDS.PARKING_BRAKE_SET, "PARKING_BRAKE_SET");
		_proxy.MapClientEventToSimEvent(EVENT_IDS.HEADING_BUG_SET, "HEADING_BUG_SET");
		_proxy.MapClientEventToSimEvent(EVENT_IDS.KOHLSMAN_SET, "KOHLSMAN_SET");
		_proxy.MapClientEventToSimEvent(EVENT_IDS.FLAPS_UP, "FLAPS_UP");
		_proxy.MapClientEventToSimEvent(EVENT_IDS.FLAPS_1, "FLAPS_1");
		_proxy.MapClientEventToSimEvent(EVENT_IDS.FLAPS_2, "FLAPS_2");
		_proxy.MapClientEventToSimEvent(EVENT_IDS.FLAPS_3, "FLAPS_3");
		_proxy.MapClientEventToSimEvent(EVENT_IDS.FLAPS_4, "FLAPS_4");
		_proxy.MapClientEventToSimEvent(EVENT_IDS.FLAPS_DOWN, "FLAPS_DOWN");

		_proxy.MapClientEventToSimEvent(EVENT_IDS.TRANSPONDER1000INC, "XPNDR_1000_INC");
		_proxy.MapClientEventToSimEvent(EVENT_IDS.TRANSPONDER100INC, "XPNDR_100_INC");
		_proxy.MapClientEventToSimEvent(EVENT_IDS.TRANSPONDER10INC, "XPNDR_10_INC");
		_proxy.MapClientEventToSimEvent(EVENT_IDS.TRANSPONDER1INC, "XPNDR_1_INC");
		_proxy.MapClientEventToSimEvent(EVENT_IDS.TRANSPONDER1000DEC, "XPNDR_1000_DEC");
		_proxy.MapClientEventToSimEvent(EVENT_IDS.TRANSPONDER100DEC, "XPNDR_100_DEC");
		_proxy.MapClientEventToSimEvent(EVENT_IDS.TRANSPONDER10DEC, "XPNDR_10_DEC");
		_proxy.MapClientEventToSimEvent(EVENT_IDS.TRANSPONDER1DEC, "XPNDR_1_DEC");

		_proxy.MapClientEventToSimEvent(EVENT_IDS.NAV_LIGHT, "TOGGLE_NAV_LIGHTS");
		_proxy.MapClientEventToSimEvent(EVENT_IDS.BEACON_LIGHT, "TOGGLE_BEACON_LIGHTS");
		_proxy.MapClientEventToSimEvent(EVENT_IDS.LANDING_LIGHT, "LANDING_LIGHTS_TOGGLE");
		_proxy.MapClientEventToSimEvent(EVENT_IDS.TAXI_LIGHT, "TOGGLE_TAXI_LIGHTS");
		_proxy.MapClientEventToSimEvent(EVENT_IDS.STROBE_LIGHT, "STROBES_TOGGLE");
		_proxy.MapClientEventToSimEvent(EVENT_IDS.PANEL_LIGHT, "PANEL_LIGHTS_TOGGLE");
		_proxy.MapClientEventToSimEvent(EVENT_IDS.RECOGNITION_LIGHT, "TOGGLE_RECOGNITION_LIGHTS");
		_proxy.MapClientEventToSimEvent(EVENT_IDS.WING_LIGHT, "TOGGLE_WING_LIGHTS");
		_proxy.MapClientEventToSimEvent(EVENT_IDS.CABIN_LIGHT, "TOGGLE_CABIN_LIGHTS");
		_proxy.MapClientEventToSimEvent(EVENT_IDS.LOGO_LIGHT, "TOGGLE_LOGO_LIGHTS");
		_proxy.MapClientEventToSimEvent(EVENT_IDS.GLARESHIELD_LIGHTPOWER, "GLARESHIELD_LIGHTS_POWER_SETTING_SET");
		_proxy.MapClientEventToSimEvent(EVENT_IDS.PANEL_LIGHTS_POWER_SETTING_SET, "LIGHT_POTENTIOMETER_SET");
		_proxy.MapClientEventToSimEvent(EVENT_IDS.CABIN_LIGHTPOWER, "CABIN_LIGHTS_POWER_SETTING_SET");
		_proxy.MapClientEventToSimEvent(EVENT_IDS.PEDESTRAL_LIGHT_POWER, "PEDESTRAL_LIGHTS_POWER_SETTING_SET");

		_proxy.MapClientEventToSimEvent(EVENT_IDS.ELECTRICAL_BATTERY_BUS_VOLTAGE, "ELECTRICAL_BATTERY_BUS_VOLTAGE");
		//sim.SubscribeToSystemEvent(MY_SIMCONENCT_EVENT_IDS.Pause, "Pause");

		_proxy.MapClientEventToSimEvent(EVENT_IDS.HEADING_GYRO_SET, "HEADING_GYRO_SET");
		_proxy.MapClientEventToSimEvent(EVENT_IDS.GYRO_DRIFT_SET, "GYRO_DRIFT_SET");

	}

	public void NoProfile()
	{
		sentToSim = true;
		OnMessageUpdate?.Invoke();
	}

	public void SendDataToSim(PlaneDataStruct data, bool BlockFuel, bool BlockLocation)
	{
		sentToSim = true;

		SendConfigurationData(data);
		if (!BlockFuel) SendFuelData(data);
		SendLightsData(data);
		if (!BlockLocation) SendLocationData(data);
		SendObsData(data);
		SendOtherData(data);
		SendPowerData(data);
		SendRadioData(data);

		OnMessageUpdate?.Invoke();
	}

	private bool CheckEnabled(string key)
	{
		return _settings.SelectedData.Exists(s => s.txt.Equals(key) && s.enabled);
	}

	private void SendConfigurationData(PlaneDataStruct data)
	{
		if (CheckEnabled(FieldText.ConfigurationParkingBrake))
		{
			_proxy.TransmitClientEvent(SimConnect.SIMCONNECT_OBJECT_ID_USER, EVENT_IDS.PARKING_BRAKE_SET, (uint)(data.parkingBrake ? 1 : 0), GROUPID.MAX, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
		}

		var elevatorTrim = CheckEnabled(FieldText.ConfigurationElevatorTrim) ? data.elevtorTrim : SimData.elevtorTrim;
		var rudderTrim = CheckEnabled(FieldText.ConfigurationRudderTrim) ? data.rudderTrim : SimData.rudderTrim;
		var aileronTrim = CheckEnabled(FieldText.ConfigurationAileronTrim) ? data.aileronTrim : SimData.aileronTrim;

		var trimData = new TrimData { elevatorTrim = elevatorTrim, rudderTrim = rudderTrim, aileronTrim = aileronTrim };
		_proxy.SetDataOnSimObject(DATA_DEFINITIONS.SimTrimData, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_DATA_SET_FLAG.DEFAULT, trimData);

		if (CheckEnabled(FieldText.ConfigurationFlaps))
		{
			switch (data.flapsIndex)
			{
				case 0:
					_proxy.TransmitClientEvent(SimConnect.SIMCONNECT_OBJECT_ID_USER, EVENT_IDS.FLAPS_UP, 0, GROUPID.MAX, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY); break;
				case 1:
					_proxy.TransmitClientEvent(SimConnect.SIMCONNECT_OBJECT_ID_USER, EVENT_IDS.FLAPS_1, 0, GROUPID.MAX, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY); break;
				case 2:
					_proxy.TransmitClientEvent(SimConnect.SIMCONNECT_OBJECT_ID_USER, EVENT_IDS.FLAPS_2, 0, GROUPID.MAX, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY); break;
				case 3:
					_proxy.TransmitClientEvent(SimConnect.SIMCONNECT_OBJECT_ID_USER, EVENT_IDS.FLAPS_3, 0, GROUPID.MAX, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY); break;
				case 4:
					_proxy.TransmitClientEvent(SimConnect.SIMCONNECT_OBJECT_ID_USER, EVENT_IDS.FLAPS_4, 0, GROUPID.MAX, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY); break;
				default:
					_proxy.TransmitClientEvent(SimConnect.SIMCONNECT_OBJECT_ID_USER, EVENT_IDS.FLAPS_DOWN, 0, GROUPID.MAX, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY); break;
			}
		}
	}

	private void SendFuelData(PlaneDataStruct data)
	{
		if (CheckEnabled(FieldText.FuelSelector))
		{
			_proxy.TransmitClientEvent(SimConnect.SIMCONNECT_OBJECT_ID_USER, EVENT_IDS.FUEL_SELECTOR_SET, (uint)data.fuelSelector, GROUPID.MAX, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
		}

		if (CheckEnabled(FieldText.FuelQtyAll))
		{
			var fuelData = new FuelData
			{
				fuelCenter = data.fuelCenter1,
				fuelCenter2 = data.fuelCenter2,
				fuelCenter3 = data.fuelCenter3,
				fuelExternal1 = data.fuelExternal1,
				fuelExternal2 = data.fuelExternal2,
				fuelLeftAux = data.fuelLeftAux,
				fuelLeftMain = data.fuelLeftMain,
				fuelLeftTip = data.fuelLeftTip,
				fuelRightAux = data.fuelRightAux,
				fuelRightMain = data.fuelRightMain,
				fuelRightTip = data.fuelRightTip
			};
			_proxy.SetDataOnSimObject(DATA_DEFINITIONS.SimPlaneFuelData, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_DATA_SET_FLAG.DEFAULT, fuelData);
		}
	}

	private void SendLightsData(PlaneDataStruct data)
	{
		if (CheckEnabled(FieldText.LightsBeacon))
			TurnOnOff(data.lightBeacon, EVENT_IDS.BEACON_LIGHT);

		if (CheckEnabled(FieldText.LightsNav))
			TurnOnOff(data.lightNav, EVENT_IDS.NAV_LIGHT);

		if (CheckEnabled(FieldText.LightsLanding))
			TurnOnOff(data.lightLanding, EVENT_IDS.LANDING_LIGHT);

		if (CheckEnabled(FieldText.LightsTaxi))
			TurnOnOff(data.lightTaxi, EVENT_IDS.TAXI_LIGHT);

		if (CheckEnabled(FieldText.LightsStrobe))
			TurnOnOff(data.lightStrobe, EVENT_IDS.STROBE_LIGHT);

		if (CheckEnabled(FieldText.LightsPanel))
			TurnOnOff(data.lightPanel, EVENT_IDS.PANEL_LIGHT);

		if (CheckEnabled(FieldText.LightsRecognition))
			TurnOnOff(data.lightRecognition, EVENT_IDS.RECOGNITION_LIGHT);

		if (CheckEnabled(FieldText.LightsWing))
			TurnOnOff(data.lightWing, EVENT_IDS.WING_LIGHT);

		if (CheckEnabled(FieldText.LightsCabin))
			TurnOnOff(data.lightCabin, EVENT_IDS.CABIN_LIGHT);

		if (CheckEnabled(FieldText.LightsLogo))
			TurnOnOff(data.lightLogo, EVENT_IDS.LOGO_LIGHT);

		if (CheckEnabled(FieldText.LightsGlareshieldPower))
			SendLightsPower(EVENT_IDS.GLARESHIELD_LIGHTPOWER, data.lightGlareShieldPct);

		if (CheckEnabled(FieldText.LightsPanelPower))
		{
			/*
			    Strange things happening
				When using eventId PANEL_LIGHTS_POWER_SETTING_SET which seems to be correct, it does nothing
				When using LIGHT_POTENTIOMETER_SET, 
					Circuit 0 and 1 turn affect Landing light and radio lights as well
					Other Circuits properly set the Panel Lights, at least in 172
			*/

			//SendLightsPower(EVENT_IDS.PANEL_LIGHTS_POWER_SETTING_SET, 100, 0);
			//SendLightsPower(EVENT_IDS.PANEL_LIGHTS_POWER_SETTING_SET, 100, 1);

			SendLightsPower(EVENT_IDS.PANEL_LIGHTS_POWER_SETTING_SET, data.lightPanelPct * 100, 2);
			SendLightsPower(EVENT_IDS.PANEL_LIGHTS_POWER_SETTING_SET, data.lightPanelPct * 100, 3);
			SendLightsPower(EVENT_IDS.PANEL_LIGHTS_POWER_SETTING_SET, data.lightPanelPct * 100, 4);
			SendLightsPower(EVENT_IDS.PANEL_LIGHTS_POWER_SETTING_SET, data.lightPanelPct * 100, 5);
		}

		if (CheckEnabled(FieldText.LightsCabinPower))
		{
			SendLightsPower(EVENT_IDS.CABIN_LIGHTPOWER, data.lightCabinPct, 1);
			SendLightsPower(EVENT_IDS.CABIN_LIGHTPOWER, data.lightCabinPct, 2);
		}

		if (CheckEnabled(FieldText.LightsPedestalPower))
			SendLightsPower(EVENT_IDS.PEDESTRAL_LIGHT_POWER, data.lightPedestralPct);
	}

	private void SendLocationData(PlaneDataStruct data)
	{
		var altitude = CheckEnabled(FieldText.LocationAltitude) ? data.altitude : SimData.altitude;
		var heading = CheckEnabled(FieldText.LocationHeading) ? data.heading : SimData.heading;
		var latitude = CheckEnabled(FieldText.LocationLatitude) ? data.latitude : SimData.latitude;
		var longitude = CheckEnabled(FieldText.LocationLongitude) ? data.longitude : SimData.longitude;
		var pitch = CheckEnabled(FieldText.LocationPitch) ? data.pitch : SimData.pitch;

		var locationData = new LocationData { altitude = altitude, heading = heading, latitude = latitude, longitude = longitude, pitch = pitch };
		_proxy.SetDataOnSimObject(DATA_DEFINITIONS.SimPlaneLocationData, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_DATA_SET_FLAG.DEFAULT, locationData);

	}

	private void SendObsData(PlaneDataStruct data)
	{
		if (CheckEnabled(FieldText.ObsAdfCard))
		{
			_proxy.TransmitClientEvent(SimConnect.SIMCONNECT_OBJECT_ID_USER, EVENT_IDS.ADF_CARD_SET, (uint)data.adfCard, GROUPID.MAX, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
		}
		if (CheckEnabled(FieldText.ObsObs1))
		{
			_proxy.TransmitClientEvent(SimConnect.SIMCONNECT_OBJECT_ID_USER, EVENT_IDS.OBS1, (uint)data.obs1, GROUPID.MAX, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
		}
		if (CheckEnabled(FieldText.ObsObs2))
		{
			_proxy.TransmitClientEvent(SimConnect.SIMCONNECT_OBJECT_ID_USER, EVENT_IDS.OBS2, (uint)data.obs2, GROUPID.MAX, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
		}
	}

	private void SendOtherData(PlaneDataStruct data)
	{
		if (CheckEnabled(FieldText.OtherTransponder))
		{
			var sim = Formatter.GetTransponder(SimData.transponder);
			var db = Formatter.GetTransponder(data.transponder);
			List<int> diffs = new List<int>();
			for (int i = 0; i < 4; i++)
			{
				int digit1 = (int)char.GetNumericValue(sim[i]);
				int digit2 = (int)char.GetNumericValue(db[i]);
				int difference = digit1 - digit2;
				diffs.Add(difference);
			}

			ApplyTransponder(diffs[0], EVENT_IDS.TRANSPONDER1000INC, EVENT_IDS.TRANSPONDER1000DEC);
			ApplyTransponder(diffs[1], EVENT_IDS.TRANSPONDER100INC, EVENT_IDS.TRANSPONDER100DEC);
			ApplyTransponder(diffs[2], EVENT_IDS.TRANSPONDER10INC, EVENT_IDS.TRANSPONDER10DEC);
			ApplyTransponder(diffs[3], EVENT_IDS.TRANSPONDER1INC, EVENT_IDS.TRANSPONDER1DEC);

			var x = SimData.transponder;
			//_proxy.TransmitClientEvent(SimConnect.SIMCONNECT_OBJECT_ID_USER, EVENT_IDS.TRANSPONDER, (uint)data.transponder, GROUPID.MAX, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
		}
		if (CheckEnabled(FieldText.OtherHeadingBug))
		{
			_proxy.TransmitClientEvent(SimConnect.SIMCONNECT_OBJECT_ID_USER, EVENT_IDS.HEADING_BUG_SET, (uint)data.headingBug, GROUPID.MAX, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
		}

		if (CheckEnabled(FieldText.OtherKolhsman))
		{
			_proxy.TransmitClientEvent(SimConnect.SIMCONNECT_OBJECT_ID_USER, EVENT_IDS.KOHLSMAN_SET, ConvertKohlsman(data.kohlsman), GROUPID.MAX, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
		}

		if (CheckEnabled(FieldText.OtherBatteryVoltage))
		{
			var batteryData = new BatteryVoltage { batteryVoltage = data.batteryVoltage };
			_proxy.SetDataOnSimObject(DATA_DEFINITIONS.SimPowerData, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_DATA_SET_FLAG.DEFAULT, batteryData);
		}

		if (CheckEnabled(FieldText.GyroDrift))
		{
			_proxy.TransmitClientEvent(SimConnect.SIMCONNECT_OBJECT_ID_USER, EVENT_IDS.HEADING_GYRO_SET, (uint)0, GROUPID.MAX, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
			Thread.Sleep(100);    // Seem to need the delay or the drift doesn't work
			_proxy.TransmitClientEvent(SimConnect.SIMCONNECT_OBJECT_ID_USER, EVENT_IDS.GYRO_DRIFT_SET, (uint)ConvertGyroDrift(data.gyroDriftError), GROUPID.MAX, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
		}
	}

	private void ApplyTransponder(int Diff, Enum IncrementEventId, Enum DecrementEventId)
	{
		var eventId = (Diff >= 0) ? DecrementEventId : IncrementEventId;

		for (int i = 0; i < Math.Abs(Diff); i++)
		{
			_proxy.TransmitClientEvent(SimConnect.SIMCONNECT_OBJECT_ID_USER, eventId, 0, GROUPID.MAX, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
		}
	}

	private void SendLightsPower(Enum EventId, double pct, uint circuit = 1)
	{
		_proxy.TransmitClientEvent_Ex1(0, EventId, GROUPID.MAX, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY, circuit, (uint)pct);
	}

	private void SendPowerData(PlaneDataStruct data)
	{
		if (CheckEnabled(FieldText.PowerMasterAlternator))
		{
			TurnOnOff(data.masterAlternator, EVENT_IDS.MASTER_ALTERNATOR);
		}

		if (CheckEnabled(FieldText.PowerMasterBattery))
		{
			TurnOnOff(data.masterBattery, EVENT_IDS.MASTER_BATTERY);
		}

		if (CheckEnabled(FieldText.PowerMasterAvionics))
		{
			TurnOnOff(data.masterAvionics, EVENT_IDS.AVIONICS_MASTER);
		}
	}

	private void SendRadioData(PlaneDataStruct data)
	{
		if (CheckEnabled(FieldText.RadiosCom1Both))
		{
			_proxy.TransmitClientEvent(SimConnect.SIMCONNECT_OBJECT_ID_USER, EVENT_IDS.COM_RADIO_SET_HZ, ConvertCom(data.com1Standby), GROUPID.MAX, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
			_proxy.TransmitClientEvent(SimConnect.SIMCONNECT_OBJECT_ID_USER, EVENT_IDS.COM_RADIO_SET_HZ, ConvertCom(data.com1Active), GROUPID.MAX, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
			_proxy.TransmitClientEvent(SimConnect.SIMCONNECT_OBJECT_ID_USER, EVENT_IDS.COM_STBY_RADIO_SET_HZ, ConvertCom(data.com1Standby), GROUPID.MAX, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
		}

		if (CheckEnabled(FieldText.RadiosCom2Both))
		{
			_proxy.TransmitClientEvent(SimConnect.SIMCONNECT_OBJECT_ID_USER, EVENT_IDS.COM2_RADIO_SET_HZ, ConvertCom(data.com2Active), GROUPID.MAX, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
			_proxy.TransmitClientEvent(SimConnect.SIMCONNECT_OBJECT_ID_USER, EVENT_IDS.COM2_STBY_RADIO_SET_HZ, ConvertCom(data.com2Standby), GROUPID.MAX, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
		}

		if (CheckEnabled(FieldText.RadiosNav1Both))
		{
			_proxy.TransmitClientEvent(SimConnect.SIMCONNECT_OBJECT_ID_USER, EVENT_IDS.NAV1_RADIO_SET_HZ, ConvertNav(data.nav1Active), GROUPID.MAX, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
			_proxy.TransmitClientEvent(SimConnect.SIMCONNECT_OBJECT_ID_USER, EVENT_IDS.NAV1_STBY_SET_HZ, ConvertNav(data.nav1Standby), GROUPID.MAX, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
		}

		if (CheckEnabled(FieldText.RadiosNav2Both))
		{
			_proxy.TransmitClientEvent(SimConnect.SIMCONNECT_OBJECT_ID_USER, EVENT_IDS.NAV2_RADIO_SET_HZ, ConvertNav(data.nav2Active), GROUPID.MAX, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
			_proxy.TransmitClientEvent(SimConnect.SIMCONNECT_OBJECT_ID_USER, EVENT_IDS.NAV2_STBY_SET_HZ, ConvertNav(data.nav2Standby), GROUPID.MAX, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
		}

		if (CheckEnabled(FieldText.RadiosAdfBoth))
		{
			var tmpStandby = data.adfStandby;
			_proxy.TransmitClientEvent(SimConnect.SIMCONNECT_OBJECT_ID_USER, EVENT_IDS.ADF_STBY_SET, ConvertAdf(data.adfActive), GROUPID.MAX, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
			_proxy.TransmitClientEvent(SimConnect.SIMCONNECT_OBJECT_ID_USER, EVENT_IDS.ADF1_RADIO_SWAP, 0, GROUPID.MAX, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
			_proxy.TransmitClientEvent(SimConnect.SIMCONNECT_OBJECT_ID_USER, EVENT_IDS.ADF_STBY_SET, ConvertAdf(tmpStandby), GROUPID.MAX, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
		}
	}

	private void TurnOnOff(bool status, EVENT_IDS id)
	{
		if (status)
		{
			_proxy.TransmitClientEvent(SimConnect.SIMCONNECT_OBJECT_ID_USER, id, 1, GROUPID.MAX, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
		}
		else
		{
			_proxy.TransmitClientEvent(SimConnect.SIMCONNECT_OBJECT_ID_USER, id, 0, GROUPID.MAX, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
		}
	}

	private static uint ConvertCom(double value)
	{
		var tmp = (uint)(Math.Round(value, 3) * 1000000);
		if (tmp.ToString().EndsWith("9") || tmp.ToString().EndsWith("4"))
		{
			tmp++;
		}

		return tmp;
	}

	private static uint ConvertNav(double value)
	{
		var tmp = (uint)(Math.Round(value, 2) * 1000000);
		if (tmp.ToString().EndsWith("9") || tmp.ToString().EndsWith("4"))
		{
			tmp++;
		}

		return tmp;
	}

	private static uint ConvertAdf(double adf)
	{
		return Dec2Bcd(Math.Round(adf, 4) * 100000);
	}

	private static uint Dec2Bcd(uint num)
	{
		return HornerScheme(num, 10, 0x10);
	}

	private static uint Dec2Bcd(double num)
	{
		return Dec2Bcd((uint)(num * 100));
	}

	private static uint HornerScheme(uint Num, uint Divider, uint Factor)
	{
		uint Remainder, Quotient, Result = 0;
		Remainder = Num % Divider;
		Quotient = Num / Divider;

		if (!(Quotient == 0 && Remainder == 0))
			Result += (HornerScheme(Quotient, Divider, Factor) * Factor) + Remainder;

		return Result;
	}

	private static uint ConvertKohlsman(double value)
	{
		var x = (uint)(value * 541.8);    //couldn't figure out how to set this, calulated this value, it is close, but sometimes off by a bit
		return x;
	}

	private static uint ConvertGyroDrift(double value)
	{
		value *= 57.2958;
		if (value < 0)
		{
			return (uint)(360 + value);
		}

		return (uint)value;
	}

	public void GetSimEnvInfo()
	{
		_proxy.RequestDataOnSimObject(DATA_REQUESTS_TYPES.SimEnvironmentReq, DATA_DEFINITIONS.SimEnvironmentDataStructure, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.ONCE, SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT, 0, 0, 0);
	}

	public bool ConnectToSim(bool Reconnect = false)
	{
		try
		{
			//  sim = new SimConnect("MainForm", mainForm.Handle, WM_USER_SIMCONNECT, null, 0);
			if (!_proxy.ConnectToSim("CoreSimConnect", WindowHandle, WM_USER_SIMCONNECT, null, 0))
			{
				return false;
			}
			SetupSimConnect();

			if (Reconnect)
			{
				OnMessageUpdate?.Invoke();
			}

			return true;
		}
		catch /*(COMException ex)*/
		{
			return false;
		}
	}

	public bool VerifyAutoSave()
	{
		return _settings.AutoSave && masterBatteryOn && sentToSim;
	}

	private void SimConnect_OnRecvSimobjectData(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA data)
	{
		switch ((DATA_REQUESTS_TYPES)data.dwRequestID)
		{
			case DATA_REQUESTS_TYPES.DataRequest:
				SimData = (PlaneDataStruct)data.dwData[0];

				if (SimData.masterBattery)
				{
					if (!masterBatteryOn)
					{
						masterBatteryOn = true;
						OnMessageUpdate?.Invoke();
					}
				}

				if (!SimData.masterBattery && VerifyAutoSave())
				{
					masterBatteryOn = false;
					OnMessageUpdate?.Invoke();
					SaveDataToDb(MasterData.title);
				}

				//update page               
				OnChangeAsync?.Invoke();
				break;

			case DATA_REQUESTS_TYPES.SimEnvironmentReq:
				MasterData = (MasterData)data.dwData[0];
				break;

			default:
				break;
		}
	}

	//void SimConnect_OnRecvEvent(SimConnect sender, SIMCONNECT_RECV_EVENT data)
	//{
	//    switch ((MY_SIMCONENCT_EVENT_IDS)(data.uEventID))
	//    {
	//        //case EVENTS.SimStart:
	//        //    int z = 7;
	//        //    break;
	//        default:
	//            break;
	//    }
	//}

	private void SimConnect_OnRecvOpen(SimConnect sender, SIMCONNECT_RECV_OPEN data)
	{
	}

	private void SimConnect_OnRecvQuit(SimConnect sender, SIMCONNECT_RECV data)
	{
		CloseConnection();
	}

	private void SimConnect_OnRecvException(SimConnect sender, SIMCONNECT_RECV_EXCEPTION data)
	{
		// return "Exception received: " + data.dwException;
	}

	private void CloseConnection()
	{
		if (_proxy.CloseConnection())
		{
			OnMessageUpdate?.Invoke();
		}
	}

	public void SaveDataToDb(string SaveName)
	{
		if ((Math.Round(SimData.longitude).Equals(0) && Math.Round(SimData.latitude).Equals(0)) || (SimData.com1Active.Equals(124.850) && SimData.com1Standby.Equals(124.85)))
		{
			DisplayMessage = "Default data not saved!";
		}
		else
		{
			_planeDataRepo.SaveDataForProfile(SaveName, SimData);
			DisplayMessage = $"Current data saved to: {SaveName}";
		}

		OnMessageUpdate?.Invoke();
	}
}