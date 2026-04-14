using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Symbol.XamarinEMDK.Barcode;
using Symbol.XamarinEMDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using Android.Media;
using Com.Zebra.Rfid.Api3;
using System.Threading;

namespace TagInventory.ScannerControllers
{
    public static class ZebraRFIDController
    {
        //Se coloco el controlador, sin embargo no se ha mandado a llamr desde el activity de inventory 12/08/2025
        private static ToneGenerator Tone;
        private static RFIDReader Reader;
        private static Readers readers;
        public static ENUM_TRIGGER_MODE TriggerMode;
        public static List<MEMORY_BANK> memoryBanksToRead;
        private static IList<ReaderDevice> availableRFIDReaderList;
        private static ReaderDevice readerDevice;
        private static EventHandler eventHandler;
        public static short AntenaPower { get; set; }
        public static string MemoryBank { get; set; }
        public static event Action<TagData> OnTagRead; // Evento para enviar tags leídos
        //public ZebraRFIDController()
        //{
        //    Tone = new ToneGenerator(Stream.Dtmf, 75);
        //}
        public static void InitZebraRFID()
        {
            try
            {
                readers ??= new Readers(Application.Context, ENUM_TRANSPORT.ServiceSerial);

                string conreaderresstring = GetAvailableReaders();
                TriggerMode = ENUM_TRIGGER_MODE.RfidMode;

                memoryBanksToRead = new List<MEMORY_BANK>();
                memoryBanksToRead.Add(MEMORY_BANK.MemoryBankTid); //El tid siempre debe ir, ya que con este nos basamos en la repetición de los tags

                if (MemoryBank != "" && MemoryBank != "Default")
                {
                    if (MemoryBank == "EPC")
                        memoryBanksToRead.Add(MEMORY_BANK.MemoryBankEpc);
                    else if (MemoryBank == "USER")
                        memoryBanksToRead.Add(MEMORY_BANK.MemoryBankUser);
                    else
                        memoryBanksToRead.Add(MEMORY_BANK.MemoryBankReserved);
                }
            }
            catch (Exception ex)
            {

            }
        }
        /// <summary>
        /// Función que indica si existen suscriptores de la clase estática
        /// </summary>
        /// <returns></returns>
        public static bool HasSuscriptors()
        {
            return OnTagRead?.GetInvocationList().Length > 0;
        }
        /// <summary>
        /// Destruye la instancia del reader de Zebra
        /// </summary>
        public static void DeinitZebraRFID()
        {
            if (Reader != null)
            {
                Reader.Events.RemoveEventsListener(eventHandler);
                Reader.Config.SetTriggerMode(ENUM_TRIGGER_MODE.RfidMode, false);
                Reader.Disconnect();
                Reader = null;
                readers = null;
            }
        }
        private static string GetAvailableReaders() //Se convierte a string debido a que encontramos que puede que no este configurada, entonces devolveremos el error de conexión.
        {
            string res = "";
            ThreadPool.QueueUserWorkItem(o =>
            {
                try
                {
                    if (readers != null && readers.AvailableRFIDReaderList != null)
                    {
                        availableRFIDReaderList = readers.AvailableRFIDReaderList;
                        if (availableRFIDReaderList.Count > 0)
                        {
                            if (Reader == null)
                            {
                                readerDevice = availableRFIDReaderList[0];
                                Reader = readerDevice.RFIDReader;
                                Reader.Connect();
                                if (Reader.IsConnected)
                                    ConfigureReader();
                            }
                        }
                    }
                }
                catch (InvalidUsageException e)
                {
                    e.PrintStackTrace();
                    res = e.VendorMessage;
                }
                catch (OperationFailureException e)
                {
                    e.PrintStackTrace();
                    res = e.VendorMessage;
                }
            });
            return res;
        }
        public static void SetRFIDPower(short Power)
        {
            if (Reader != null)
            {
                Antennas.Config antenna = Reader.Config.Antennas.GetAntennaConfig(1);
                //antenna.TransmitPowerIndex = configuraciones.RFIDConf.zebra.AntennaPower;
                antenna.TransmitPowerIndex = Power;
                AntenaPower = Power;
                Reader.Config.Antennas.SetAntennaConfig(1, antenna);
            }
        }
        private static void ConfigureReader()
        {
            try
            {
                if (Reader.IsConnected)
                {
                    TriggerInfo triggerInfo = new TriggerInfo();
                    triggerInfo.StartTrigger.TriggerType = START_TRIGGER_TYPE.StartTriggerTypeImmediate;
                    triggerInfo.StopTrigger.TriggerType = STOP_TRIGGER_TYPE.StopTriggerTypeImmediate;
                    try
                    {
                        eventHandler ??= new EventHandler(Reader);

                        Reader.Events.AddEventsListener(eventHandler);
                        Reader.Events.SetHandheldEvent(true);

                        Reader.Events.SetTagReadEvent(true);
                        Reader.Events.SetAttachTagDataWithReadEvent(false);

                        Reader.Events.SetInventoryStartEvent(true);
                        Reader.Events.SetInventoryStopEvent(true);
                        Reader.Events.SetOperationEndSummaryEvent(true);
                        Reader.Events.SetReaderDisconnectEvent(true);
                        Reader.Events.SetBatteryEvent(true);
                        Reader.Events.SetPowerEvent(true);
                        Reader.Events.SetTemperatureAlarmEvent(true);
                        Reader.Events.SetBufferFullEvent(true);

                        Reader.Config.SetTriggerMode(ENUM_TRIGGER_MODE.RfidMode, true);
                        TriggerMode = ENUM_TRIGGER_MODE.RfidMode;
                        ////Para cambiar a código de barras
                        //Reader.Config.SetTriggerMode(ENUM_TRIGGER_MODE.BarcodeMode, true);

                        Reader.Config.StartTrigger = triggerInfo.StartTrigger;
                        Reader.Config.StopTrigger = triggerInfo.StopTrigger;

                        //Al parecer, con estas lineas de código, evitamos que se lea demasiado rápido, esto ayuda a no ciclar la aplicación 23/05/2024
                        Antennas.SingulationControl ATS = Reader.Config.Antennas.GetSingulationControl(1);
                        ATS.Session = SESSION.SessionS1;
                        ATS.Action.SLFlag = SL_FLAG.SlAll;
                        ATS.Action.InventoryState = INVENTORY_STATE.InventoryStateA;
                        Reader.Config.Antennas.SetSingulationControl(1, ATS);

                        Antennas.Config antenna = Reader.Config.Antennas.GetAntennaConfig(1);
                        //antenna.TransmitPowerIndex = configuraciones.RFIDConf.zebra.AntennaPower;
                        antenna.TransmitPowerIndex = AntenaPower;
                        Reader.Config.Antennas.SetAntennaConfig(1, antenna);

                    }
                    catch (InvalidUsageException e)
                    {
                        e.PrintStackTrace();
                    }
                    catch (OperationFailureException e)
                    {
                        e.PrintStackTrace();
                    }
                }
            }
            catch (Exception ex)
            {
            }
        }
        private static void Beep()
        {
            try
            {
                Tone ??= new ToneGenerator(Stream.Dtmf, 100);
                Tone.StartTone(Android.Media.Tone.PropBeep);
            }
            catch { }
        }
        public class EventHandler : Java.Lang.Object, IRfidEventsListener
        {
            public EventHandler(RFIDReader Reader)
            {
            }
            public void EventReadNotify(RfidReadEvents p0)
            {
                try
                {
                    TagData[] ReadTags = Reader.Actions.GetReadTags(500);
                    ReadTags = ReadTags.GroupBy(t => t.TagID).Select(grp => grp.First()).ToArray();
                    Beep();
                    if (ReadTags != null)
                    {
                        foreach (TagData Tag in ReadTags)
                            OnTagRead?.Invoke(Tag);
                    }
                }
                catch (Exception ex)
                {
                }
            }
            public void EventStatusNotify(RfidStatusEvents rfidStatusEvents)
            {
                try
                {
                    if (rfidStatusEvents.StatusEventData.StatusEventType == STATUS_EVENT_TYPE.HandheldTriggerEvent)
                    {
                        if (TriggerMode != ENUM_TRIGGER_MODE.RfidMode) return; //Agregado 02/05/2023 Con esto evitamos que se ejecute el evento de disparo de RFID
                        if (rfidStatusEvents.StatusEventData.HandheldTriggerEventData.HandheldEvent == HANDHELD_TRIGGER_EVENT_TYPE.HandheldTriggerPressed)
                        {
                            //ThreadPool.QueueUserWorkItem(o =>
                            //{
                            try
                            {
                                if (memoryBanksToRead != null)
                                {
                                    foreach (MEMORY_BANK bank in memoryBanksToRead)
                                    {
                                        TagAccess ta = new TagAccess();
                                        TagAccess.Sequence sequence = new TagAccess.Sequence(ta, ta);
                                        TagAccess.Sequence.Operation op = new TagAccess.Sequence.Operation(sequence);
                                        op.AccessOperationCode = ACCESS_OPERATION_CODE.AccessOperationRead;
                                        op.ReadAccessParams.MemoryBank = bank ?? throw new ArgumentNullException(nameof(bank));
                                        Reader.Actions.TagAccess.OperationSequence.Add(op);
                                    }
                                }
                                if (memoryBanksToRead != null)
                                    Reader.Actions.TagAccess.OperationSequence.PerformSequence();
                                else
                                    Reader.Actions.Inventory.Perform();
                            }
                            catch (InvalidUsageException e)
                            {
                                e.PrintStackTrace();
                            }
                            catch (OperationFailureException e)
                            {
                                e.PrintStackTrace();
                            }
                            //});
                        }
                        if (rfidStatusEvents.StatusEventData.HandheldTriggerEventData.HandheldEvent == HANDHELD_TRIGGER_EVENT_TYPE.HandheldTriggerReleased)
                        {
                            //ThreadPool.QueueUserWorkItem(o =>
                            //{
                            try
                            {
                                if (memoryBanksToRead != null)
                                    Reader.Actions.TagAccess.OperationSequence.StopSequence();
                                else
                                    Reader.Actions.Inventory.Stop();
                            }
                            catch (InvalidUsageException e)
                            {
                                e.PrintStackTrace();
                            }
                            catch (OperationFailureException e)
                            {
                                e.PrintStackTrace();
                            }
                            //});
                        }
                    }
                }
                catch { }
            }
        }
    }
}