// DbxHost.cs — x86-хост P-CAD DBX API с JSON-протоколом stdin/stdout.
//
// Dbx32.dll — 32-битная DDE-клиентская библиотека P-CAD 2002. Она не читает
// файлы сама, а подключается к уже запущенному Sch.exe/Pcb.exe (TOpenDesign
// с pAppName "sch"/"pcb") и оперирует дизайном, открытым в редакторе.
//
// Протокол: на stdin — по одной JSON-команде на строку, на stdout — по одному
// JSON-ответу на строку: {"ok":true,"data":...} или {"ok":false,"error":N,"message":"..."}.
// Команды: ping, open {app,wait_sec}, close, save, info,
//          components {pins,attrs}, nets {nodes}, comp {refdes,pins,attrs},
//          net {name,nodes}, shutdown.
//
// Сборка: build.bat (csc.exe .NET Framework 4, /platform:x86).
// Синтаксис намеренно C# 4: компилируется штатным csc.exe без Visual Studio.
//
// Структуры и константы перенесены из C:\Program Files (x86)\PCAD2002\Dbx\Dbx32.h
// (P-CAD 2002 DBX API, Rev от 2002). Pack=1 и ANSI-строки — обязательны.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Web.Script.Serialization;

internal static class Dbx
{
    public const string DllPath = @"C:\Program Files (x86)\PCAD2002\Dbx\Dbx32.dll";

    public const int LANGUAGE = 0;
    public const int VERSION = 16000;

    public const int OK = 0;
    public const int NO_MORE_ITEMS = 32163;
    public const int NO_MORE_NETS = 32162;
    public const int NO_MORE_COMPONENTS = 32168;

    public const int ITEM_PIN = 39;
    public const int ITEM_PORT = 50;

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct TCoord { public int x; public int y; }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct TBoundRect { public TCoord lowerLeft; public TCoord upperRight; }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct DbxContext
    {
        public IntPtr hConv;
        public int appInst;
        public int version;
        public int language;
        public IntPtr hWnd;
        public IntPtr hMmf;
        public IntPtr pMmf;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct TArc
    {
        public int itemId; public int width; public int radius;
        public TCoord centerPt; public int startAng; public int sweepAng;
        public TBoundRect boundRect; public int netId; public int layerId;
        public int isHighlighted; public int isFixed;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct TAttribute
    {
        public int itemId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)] public string type;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string value;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 512)] public string formula;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string comment;
        public int typeLength; public int valueLength; public int formulaLength; public int commentLength;
        public int textStyleId; public int justPoint;
        public TCoord refPoint; public TBoundRect boundRect;
        public int rotationAng; public int compId; public int netId; public int netClassId; public int layerId;
        public int isFlipped; public int isHighlighted; public int isVisible; public int units;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct TBus
    {
        public int itemId; public TCoord startPt; public TCoord endPt; public int layerId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)] public string busName;
        public int isFlipped; public int isHighlighted; public int isNameVisible;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct TClassToClass
    {
        public int netClassId1; public int netClassId2;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)] public string netClassName1;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)] public string netClassName2;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct TComponent
    {
        public int compId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)] public string refDes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)] public string compType;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)] public string value;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)] public string patternName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string libraryName;
        public TCoord refPoint; public TBoundRect boundRect;
        public int rotationAng; public int numberPads; public int numberPins; public int numberParts;
        public int isAlpha; public int isFlipped; public int isHighlighted; public int isHetero;
        public int connectionType; public int isFixed; public int numberGraphics;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)] public string currentGraphicName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)] public string defaultGraphicName;
        public int isAutoSwapGraphic;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct TCompGraphic
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)] public string graphicName;
        public int numberPads; public int numberAttrs; public int numberItems;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct TCompPin
    {
        public int compPinNumber;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)] public string pinDes;
        public int gateNumber; public int symPinNumber; public int padNumber;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)] public string pinName;
        public int gateEq; public int pinEq; public int electype;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct TDesign
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)] public string designName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)] public string title;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)] public string designer;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)] public string version;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)] public string lastModifiedDate;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)] public string lastModifiedTime;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)] public string drawingNumber;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)] public string guidString;
        public TCoord workSpaceSize; public TBoundRect layerExtents;
        public int absGridId; public int relGridId; public int isGridAbsolute; public int userUnits;
        public TCoord relGridOrigin; public int isModified; public int solderFlowDir;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct TDetail
    {
        public int itemId; public TCoord refPoint; public int layerId; public TBoundRect boundRect;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string title;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string subTitle;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string fileName;
        public int textStyleId;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct TDiagram
    {
        public int itemId; public TCoord refPoint; public int layerId; public TBoundRect boundRect;
        public int diagramType;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string title;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string subTitle;
        public int textStyleId; public int lineWidth;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct TField
    {
        public int itemId; public int textStyleId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string text;
        public int layerId; public int justPoint; public TCoord refPoint; public int rotationAng;
        public int isFlipped; public int isHighlighted; public int isVisible; public int fieldKeyType;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct TGrid
    {
        public int gridId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string gridSpacing;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct TLayer
    {
        public int layerId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)] public string layerName;
        public int layerType; public int layerBias; public int planeNetId;
        public int lineLineClearance; public int padLineClearance; public int padPadClearance;
        public int viaPadClearance; public int viaLineClearance; public int viaViaClearance;
        public int isEnabled; public int layerPosition;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct TLine
    {
        public int itemId; public int lineType; public int width;
        public TCoord startPt; public TCoord endPt; public TBoundRect boundRect;
        public int netId; public int layerId; public int isHighlighted; public int isFixed;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct TMetaFile
    {
        public int itemId; public TCoord refPoint; public int layerId; public TBoundRect boundRect;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct TNet
    {
        public int netId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)] public string netName;
        public int nodeCount; public int length; public int isPlane;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct TNetClass
    {
        public int netClassId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)] public string netClassName;
        public int numberOfNets;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct TPad
    {
        public int itemId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)] public string compRefDes;
        public int padStyleId; public TCoord center; public int layerId;
        public int isFlipped; public int rotationAng; public int isHighlighted; public int netId;
        public TBoundRect boundRect; public TCompPin compPin;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)] public string defaultPinDes;
        public int isFixed;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct TPadViaShape
    {
        public int layerId; public int layerType; public int styleType; public int shape;
        public int holeDia; public int width; public int height; public int outerDia;
        public int innerDia; public int spokeWidth; public int isPourNoConn;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct TPadViaStyle
    {
        public int styleId; public int styleType;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)] public string name;
        public int holeDia; public int isHolePlated; public int xOffset; public int yOffset;
        public int holeStartLayer; public int holeEndLayer; public int drillSymbol;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct TPattern
    {
        public int itemId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)] public string patternName;
        public int rotationAng; public int isFlipped; public int isHighlighted;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct TPin
    {
        public int itemId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)] public string compRefDes;
        public int outsideStyle; public int outsideEdgeStyle; public int insideStyle; public int insideEdgeStyle;
        public TCoord refPoint; public int layerId; public int isFlipped; public int isHighlighted;
        public int rotationAng; public int netId; public TBoundRect boundRect; public int pinLength;
        public TCompPin compPin;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)] public string defaultPinDes;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct TPoint
    {
        public int itemId; public int x; public int y; public int pointType; public int number;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string textInfo;
        public int layerId; public int isFlipped; public int isVisible; public int isHighlighted;
        public int ruleCategory; public int ruleType; public int violationType;
        public int placementSide; public int isSnapToCenter; public int netId; public int isFixed;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct TPoly
    {
        public int itemId; public int polyType; public int numPts; public TBoundRect boundRect;
        public int netId; public int layerId; public int isHighlighted;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct TPort
    {
        public int itemId; public int netId; public int portType; public int pinLength;
        public int rotationAng; public int isFlipped; public int isHighlighted;
        public int textStyleId; public int layerId; public TCoord refPoint;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct TPour
    {
        public int itemId; public int lineSpacing; public int lineWidth; public int thermalType;
        public int pourType; public int numPts; public TBoundRect boundRect;
        public int netId; public int layerId; public int isFlooded; public int isHighlighted; public int isFixed;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct TPrintJob
    {
        public int isSelected;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string jobName;
        public int isRotated;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct TRoom
    {
        public int roomId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)] public string roomName;
        public int layerId; public int numberOfIncludedComps; public int numberOfExcludedComps;
        public TBoundRect boundRect; public int placementSide; public int isFixed;
        public int isFlipped; public int isHighlighted; public int roomFillPattern;
        public TCoord refPoint; public int rotationAngle;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct TSymbol
    {
        public int symbolId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)] public string symbolName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)] public string refDes;
        public int numberPins; public int partNumber; public int altType;
        public TCoord refPoint; public TBoundRect boundRect; public int rotationAng;
        public int isFlipped; public int isHighlighted; public int layerId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)] public string compType;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)] public string libraryName;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct TTable
    {
        public int itemId; public TCoord refPoint; public int layerId; public TBoundRect boundRect;
        public int rotationAng; public int isHighlighted; public int isFlipped; public int tableType;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string tableTitle;
        public int textStyleId; public int lineWidth;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct TText
    {
        public int itemId; public int textStyleId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string text;
        public int layerId; public int justPoint; public TCoord refPoint; public TBoundRect boundRect;
        public int rotationAng; public int isFlipped; public int isVisible; public int isHighlighted;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct TTextStyle
    {
        public int styleId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)] public string name;
        public int strokePenWidth; public int strokeHeight;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)] public string tTypeFaceName;
        public int tTypeHeight; public int isTrueTypeAllowed; public int isDisplayTrueType;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct TVia
    {
        public int itemId; public int netId; public TCoord center; public int rotationAng;
        public int viaStyleId; public TBoundRect boundRect;
        public int isFlipped; public int isHighlighted; public int isFixed;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct TWire
    {
        public int itemId; public TCoord startPt; public TCoord endPt; public int layerId;
        public int isFlipped; public int isHighlighted; public int isNameVisible;
        public int netId; public int width;
    }

    // TItem — ПОСЛЕДОВАТЕЛЬНАЯ (не union!) структура: itemType + все типы подряд.
    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct TItem
    {
        public int itemType;
        public TArc arc;
        public TAttribute attribute;
        public TBus bus;
        public TClassToClass classToClass;
        public TComponent component;
        public TCompPin compPin;
        public TDesign design;
        public TDetail detail;
        public TDiagram diagram;
        public TField field;
        public TGrid grid;
        public TLayer layer;
        public TLine line;
        public TMetaFile metaFile;
        public TNet net;
        public TNetClass netClass;
        public TPad pad;
        public TPadViaShape padViaShape;
        public TPadViaStyle padViaStyle;
        public TPattern pattern;
        public TPin pin;
        public TPoint point;
        public TPoly poly;
        public TPort port;
        public TPour pour;
        public TPrintJob printJob;
        public TRoom room;
        public TSymbol symbol;
        public TTable table;
        public TText text;
        public TTextStyle textStyle;
        public TVia via;
        public TWire wire;
        public TCompGraphic compGraphic;
    }

    [DllImport(DllPath, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
    public static extern int TOpenDesign(int language, int version, string pAppName, ref DbxContext pContext);

    [DllImport(DllPath, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
    public static extern int TCloseDesign(ref DbxContext pContext, string pDesignName);

    [DllImport(DllPath, CallingConvention = CallingConvention.StdCall)]
    public static extern int TSaveDesign(ref DbxContext pContext);

    [DllImport(DllPath, CallingConvention = CallingConvention.StdCall)]
    public static extern int TGetDesignInfo(ref DbxContext pContext, ref TDesign pDesignInfo);

    [DllImport(DllPath, CallingConvention = CallingConvention.StdCall)]
    public static extern int TGetFirstComponent(ref DbxContext pContext, ref TComponent pComponent);

    [DllImport(DllPath, CallingConvention = CallingConvention.StdCall)]
    public static extern int TGetNextComponent(ref DbxContext pContext, ref TComponent pComponent);

    [DllImport(DllPath, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
    public static extern int TGetCompByRefDes(ref DbxContext pContext, string pCompRefDes, ref TComponent pComponent);

    [DllImport(DllPath, CallingConvention = CallingConvention.StdCall)]
    public static extern int TGetFirstNet(ref DbxContext pContext, ref TNet pNet);

    [DllImport(DllPath, CallingConvention = CallingConvention.StdCall)]
    public static extern int TGetNextNet(ref DbxContext pContext, ref TNet pNet);

    [DllImport(DllPath, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
    public static extern int TGetNetByName(ref DbxContext pContext, string pNetName, ref TNet pNet);

    [DllImport(DllPath, CallingConvention = CallingConvention.StdCall)]
    public static extern int TGetFirstNetNode(ref DbxContext pContext, int netId, ref TItem pNetNode);

    [DllImport(DllPath, CallingConvention = CallingConvention.StdCall)]
    public static extern int TGetNextNetNode(ref DbxContext pContext, ref TItem pNetNode);

    [DllImport(DllPath, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
    public static extern int TGetFirstCompPin(ref DbxContext pContext, string pCompRefDes, ref TPin pTPin);

    [DllImport(DllPath, CallingConvention = CallingConvention.StdCall)]
    public static extern int TGetNextCompPin(ref DbxContext pContext, ref TPin pTPin);

    [DllImport(DllPath, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
    public static extern int TGetFirstCompAttribute(ref DbxContext pContext, string pCompRefDes, ref TAttribute pAttr);

    [DllImport(DllPath, CallingConvention = CallingConvention.StdCall)]
    public static extern int TGetNextCompAttribute(ref DbxContext pContext, ref TAttribute pAttr);

    public static readonly Dictionary<int, string> ErrorNames = new Dictionary<int, string>
    {
        {0, "DBX_OK"},
        {32001, "DBX_CONNECTION_ERROR"}, {32002, "DBX_DISCONNECT_ERROR"},
        {32003, "DBX_BAD_SERVER_DATA"}, {32004, "DBX_SERVER_ERROR"},
        {32005, "DBX_FATAL_ERROR"}, {32006, "DBX_VERSION_INCOMPATIBLE"},
        {32007, "DBX_CLIENT_ERROR"}, {32008, "DBX_SECURITY_ERROR"},
        {32009, "DBX_FLIP_ERROR"}, {32010, "DBX_ROTATE_ERROR"},
        {32011, "DBX_MOVE_ERROR"}, {32012, "DBX_HIGHLIGHT_ERROR"},
        {32013, "DBX_DELETE_ERROR"}, {32014, "DBX_MODIFY_ERROR"},
        {32015, "DBX_CREATE_ERROR"}, {32016, "DBX_DATABASE_ERROR"},
        {32101, "DBX_INVALID_CONV_HANDLE"}, {32102, "DBX_SERVER_TERMINATED"},
        {32103, "DBX_LOW_MEMORY"}, {32104, "DBX_ALREADY_CONNECTED"},
        {32105, "DBX_NO_CONNECTION"}, {32106, "DBX_SERVER_BUSY"},
        {32121, "DBX_ILLEGAL_OP"}, {32122, "DBX_BAD_INPUT"},
        {32123, "DBX_ARRAY_TOO_SMALL"}, {32124, "DBX_INVALID_ITEM_ID"},
        {32141, "DBX_ITEM_NOT_FOUND"}, {32142, "DBX_ITEM_NOT_SUPPORTED"},
        {32143, "DBX_ILLEGAL_ITEM"}, {32145, "DBX_GETFIRST_NOT_CALLED"},
        {32146, "DBX_GETCOMP_NOT_CALLED"}, {32147, "DBX_GETNET_NOT_CALLED"},
        {32161, "DBX_NO_MORE_LAYERS"}, {32162, "DBX_NO_MORE_NETS"},
        {32163, "DBX_NO_MORE_ITEMS"}, {32168, "DBX_NO_MORE_COMPONENTS"},
        {32176, "DBX_INVALID_NET_ID"}, {32177, "DBX_INVALID_NETNAME"},
        {32181, "DBX_INVALID_REFDES"}, {32215, "DBX_COMPONENT_NOT_FOUND"},
        {32217, "DBX_NET_NOT_FOUND"}, {32235, "DBX_PINDES_NOT_FOUND"},
        {32239, "DBX_FILE_OPEN_FAILURE"},
    };

    public static string ErrorText(int code)
    {
        string name;
        return ErrorNames.TryGetValue(code, out name) ? name : "DBX_STATUS_" + code;
    }
}

internal static class Host
{
    private static Dbx.DbxContext _ctx;
    private static bool _open;
    private static readonly JavaScriptSerializer Json = new JavaScriptSerializer { MaxJsonLength = 64 * 1024 * 1024 };

    private static Dictionary<string, object> Ok(object data)
    {
        return new Dictionary<string, object> { { "ok", true }, { "data", data } };
    }

    private static Dictionary<string, object> Fail(int code, string where)
    {
        return new Dictionary<string, object>
        {
            { "ok", false },
            { "error", code },
            { "message", where + ": [" + code + "] " + Dbx.ErrorText(code) },
        };
    }

    private static int Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        string line;
        while ((line = Console.In.ReadLine()) != null)
        {
            Dictionary<string, object> resp;
            try
            {
                var cmd = Json.Deserialize<Dictionary<string, object>>(line);
                resp = Dispatch(cmd);
            }
            catch (Exception ex)
            {
                resp = new Dictionary<string, object>
                {
                    { "ok", false }, { "error", -1 }, { "message", "host: " + ex.Message },
                };
            }
            Console.Out.WriteLine(Json.Serialize(resp));
            Console.Out.Flush();
            if (resp.ContainsKey("bye"))
                break;
        }
        if (_open)
            Dbx.TCloseDesign(ref _ctx, "");
        return 0;
    }

    private static bool Flag(Dictionary<string, object> cmd, string name, bool dflt)
    {
        object v;
        if (!cmd.TryGetValue(name, out v) || v == null) return dflt;
        return Convert.ToBoolean(v);
    }

    private static Dictionary<string, object> Dispatch(Dictionary<string, object> cmd)
    {
        string name = Convert.ToString(cmd["cmd"]);
        switch (name)
        {
            case "ping":
                return Ok(new Dictionary<string, object> { { "host", "DbxHost" }, { "dll", Dbx.DllPath } });

            case "open":
            {
                string app = cmd.ContainsKey("app") ? Convert.ToString(cmd["app"]) : "sch";
                int waitSec = cmd.ContainsKey("wait_sec") ? Convert.ToInt32(cmd["wait_sec"]) : 0;
                var deadline = DateTime.Now.AddSeconds(waitSec);
                int st;
                while (true)
                {
                    st = Dbx.TOpenDesign(Dbx.LANGUAGE, Dbx.VERSION, app, ref _ctx);
                    if (st == Dbx.OK || DateTime.Now >= deadline)
                        break;
                    System.Threading.Thread.Sleep(500);
                }
                if (st != Dbx.OK)
                    return Fail(st, "TOpenDesign");
                _open = true;
                return Ok(new Dictionary<string, object> { { "app", app }, { "ctxVersion", _ctx.version } });
            }

            case "close":
            {
                if (!_open) return Ok(null);
                int st = Dbx.TCloseDesign(ref _ctx, "");
                _open = false;
                return st == Dbx.OK ? Ok(null) : Fail(st, "TCloseDesign");
            }

            case "save":
            {
                int st = Dbx.TSaveDesign(ref _ctx);
                return st == Dbx.OK ? Ok(null) : Fail(st, "TSaveDesign");
            }

            case "info":
            {
                var d = new Dbx.TDesign();
                int st = Dbx.TGetDesignInfo(ref _ctx, ref d);
                if (st != Dbx.OK) return Fail(st, "TGetDesignInfo");
                return Ok(new Dictionary<string, object>
                {
                    { "designName", d.designName }, { "title", d.title },
                    { "designer", d.designer }, { "version", d.version },
                    { "lastModifiedDate", d.lastModifiedDate }, { "lastModifiedTime", d.lastModifiedTime },
                    { "drawingNumber", d.drawingNumber },
                    { "userUnits", d.userUnits }, { "isModified", d.isModified },
                    { "workSpaceSize", new[] { d.workSpaceSize.x, d.workSpaceSize.y } },
                });
            }

            case "components":
            {
                bool withPins = Flag(cmd, "pins", false);
                bool withAttrs = Flag(cmd, "attrs", false);
                var comps = new List<Dbx.TComponent>();
                var c = new Dbx.TComponent();
                int st = Dbx.TGetFirstComponent(ref _ctx, ref c);
                while (st == Dbx.OK)
                {
                    comps.Add(c);
                    st = Dbx.TGetNextComponent(ref _ctx, ref c);
                }
                if (st != Dbx.NO_MORE_COMPONENTS && st != Dbx.NO_MORE_ITEMS)
                    return Fail(st, "TGetNextComponent");
                var list = new List<object>();
                foreach (var comp in comps)
                    list.Add(CompDict(comp, withPins, withAttrs));
                return Ok(new Dictionary<string, object> { { "count", comps.Count }, { "components", list } });
            }

            case "comp":
            {
                string refdes = Convert.ToString(cmd["refdes"]);
                var c = new Dbx.TComponent();
                int st = Dbx.TGetCompByRefDes(ref _ctx, refdes, ref c);
                if (st != Dbx.OK) return Fail(st, "TGetCompByRefDes");
                return Ok(CompDict(c, Flag(cmd, "pins", true), Flag(cmd, "attrs", true)));
            }

            case "nets":
            {
                bool withNodes = Flag(cmd, "nodes", false);
                var nets = new List<Dbx.TNet>();
                var n = new Dbx.TNet();
                int st = Dbx.TGetFirstNet(ref _ctx, ref n);
                while (st == Dbx.OK)
                {
                    nets.Add(n);
                    st = Dbx.TGetNextNet(ref _ctx, ref n);
                }
                if (st != Dbx.NO_MORE_NETS && st != Dbx.NO_MORE_ITEMS)
                    return Fail(st, "TGetNextNet");
                var list = new List<object>();
                foreach (var net in nets)
                    list.Add(NetDict(net, withNodes));
                return Ok(new Dictionary<string, object> { { "count", nets.Count }, { "nets", list } });
            }

            case "net":
            {
                string netName = Convert.ToString(cmd["name"]);
                var n = new Dbx.TNet();
                int st = Dbx.TGetNetByName(ref _ctx, netName, ref n);
                if (st != Dbx.OK) return Fail(st, "TGetNetByName");
                return Ok(NetDict(n, Flag(cmd, "nodes", true)));
            }

            case "shutdown":
            {
                if (_open) { Dbx.TCloseDesign(ref _ctx, ""); _open = false; }
                var r = Ok(null);
                r["bye"] = true;
                return r;
            }

            default:
                return new Dictionary<string, object>
                {
                    { "ok", false }, { "error", -2 }, { "message", "unknown cmd: " + name },
                };
        }
    }

    private static Dictionary<string, object> CompDict(Dbx.TComponent c, bool withPins, bool withAttrs)
    {
        var d = new Dictionary<string, object>
        {
            { "compId", c.compId }, { "refDes", c.refDes }, { "compType", c.compType },
            { "value", c.value }, { "patternName", c.patternName }, { "libraryName", c.libraryName },
            { "numberPins", c.numberPins }, { "numberParts", c.numberParts },
            { "connectionType", c.connectionType }, { "isHetero", c.isHetero },
        };
        if (withPins)
        {
            var pins = new List<object>();
            var p = new Dbx.TPin();
            int st = Dbx.TGetFirstCompPin(ref _ctx, c.refDes, ref p);
            while (st == Dbx.OK)
            {
                pins.Add(new Dictionary<string, object>
                {
                    { "pinDes", p.compPin.pinDes }, { "pinName", p.compPin.pinName },
                    { "netId", p.netId }, { "electype", p.compPin.electype },
                    { "gateNumber", p.compPin.gateNumber }, { "symPinNumber", p.compPin.symPinNumber },
                    { "x", p.refPoint.x }, { "y", p.refPoint.y },
                });
                st = Dbx.TGetNextCompPin(ref _ctx, ref p);
            }
            d["pins"] = pins;
        }
        if (withAttrs)
            d["attrs"] = AttrList(c.refDes);
        return d;
    }

    private static List<object> AttrList(string refdes)
    {
        var attrs = new List<object>();
        var a = new Dbx.TAttribute();
        int st = Dbx.TGetFirstCompAttribute(ref _ctx, refdes, ref a);
        while (st == Dbx.OK)
        {
            attrs.Add(new Dictionary<string, object>
            {
                { "type", a.type }, { "value", a.value }, { "isVisible", a.isVisible },
            });
            st = Dbx.TGetNextCompAttribute(ref _ctx, ref a);
        }
        return attrs;
    }

    private static Dictionary<string, object> NetDict(Dbx.TNet n, bool withNodes)
    {
        var d = new Dictionary<string, object>
        {
            { "netId", n.netId }, { "netName", n.netName }, { "nodeCount", n.nodeCount },
        };
        if (withNodes)
        {
            var nodes = new List<object>();
            var item = new Dbx.TItem();
            int st = Dbx.TGetFirstNetNode(ref _ctx, n.netId, ref item);
            while (st == Dbx.OK)
            {
                if (item.itemType == Dbx.ITEM_PIN)
                {
                    nodes.Add(new Dictionary<string, object>
                    {
                        { "kind", "pin" },
                        { "refDes", item.pin.compRefDes },
                        { "pinDes", item.pin.compPin.pinDes },
                        { "pinName", item.pin.compPin.pinName },
                    });
                }
                else if (item.itemType == Dbx.ITEM_PORT)
                {
                    nodes.Add(new Dictionary<string, object>
                    {
                        { "kind", "port" }, { "netId", item.port.netId },
                    });
                }
                else
                {
                    nodes.Add(new Dictionary<string, object>
                    {
                        { "kind", "item" }, { "itemType", item.itemType },
                    });
                }
                st = Dbx.TGetNextNetNode(ref _ctx, ref item);
            }
            d["nodes"] = nodes;
        }
        return d;
    }
}
