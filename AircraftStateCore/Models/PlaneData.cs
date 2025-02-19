using System.Runtime.InteropServices;

namespace AircraftStateCore.Models;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
public struct PlaneDataStruct
{
	public double latitude;   //settable
	public double longitude;  //settable
	public int altitude;    //settable
	public double heading;    //settable
	public double pitch;      //settable

	public double com1Active;
	public double com1Standby;
	public double com2Active;
	public double com2Standby;
	public double nav1Active;
	public double nav1Standby;
	public double nav2Active;
	public double nav2Standby;
	public double adfActive;
	public double adfStandby;

	public double obs1;
	public double obs2;
	public double adfCard;   //test in 172

	public double fuelCenter1;    //settable
	public double fuelCenter2;    //settable
	public double fuelCenter3;    //settable
	public double fuelExternal1;    //settable
	public double fuelExternal2;    //settable
	public double fuelLeftAux;    //settable
	public double fuelLeftMain;    //settable
	public double fuelLeftTip;    //settable
	public double fuelRightAux;    //settable
	public double fuelRightMain;    //settable
	public double fuelRightTip;    //settable
	public int fuelSelector;

	public bool parkingBrake;
	public double kohlsman;
	public double headingBug;

	public int flapsIndex;
	public double elevtorTrim;
	public double rudderTrim;
	public double aileronTrim;

	public double gyroDriftError;
	public double headingIndicator;

	public bool masterBattery;
	public bool masterAlternator;
	public bool masterAvionics;

	public double batteryVoltage;  //settable

	public bool lightNav;
	public bool lightBeacon;
	public bool lightLanding;
	public bool lightTaxi;
	public bool lightStrobe;
	public bool lightPanel;
	public bool lightRecognition;
	public bool lightWing;
	public bool lightCabin;
	public bool lightLogo;
	public double lightGlareShieldPct;
	public double lightCabinPct;
	public double lightPedestralPct;
	public double lightPanelPct;

	public uint transponder;

	public bool junk;

	//FLAPS HANDLE INDEX	//settable
	public bool validData;
}