using Android.Media;
using Com.Rscja.Deviceapi;
using Com.Rscja.Deviceapi.Entity;
using Com.Zebra.Rfid.Api3;
using System;
using TagInventory.Modelos;


namespace TagInventory.ScannerControllers
{
    public class ChainwayRFIDController
    {
        private readonly RFIDWithUHFUART uhfApi = RFIDWithUHFUART.Instance;
        private ToneGenerator Tone;

        public event Action<UHFTAGInfo> OnTagsRead;
        public bool Reading { get; set; }
        public bool Init()
        {
            //int bank = RFIDWithUHFUART.InterfaceConsts.BankTID;
            //bool re = uhfApi.SetFilter(bank, 0, 0, "00");
            return uhfApi.Init();
        }
        public bool SetEPCAndTIDMode()
        {
            return uhfApi.SetEPCAndTIDMode();
        }
        public bool Free() => uhfApi.Free();
        public bool StartInventory()
        {
            Reading = true;
            return uhfApi.StartInventoryTag();
        }
        public bool StopInventory()
        {
            Reading = false;
            return uhfApi.StopInventory();
        }

        public bool SetPower(int power) => uhfApi.SetPower(power);
        public int GetPower() => uhfApi.Power;
        public string WriteTag(string pwdstr, int bank, string address, int ctr, string strData)
        {
            int ptr = int.Parse(address);
            int PrevPwr = GetPower(); //Guardamos la potencia previa, para despues colocarle la que tenia
            SetPower(30); //Seteamos la potencia al maximo
            //bool write = uhfApi.WriteData(pwdstr, bank, ptr, ctr, strData);
            bool write = uhfApi.WriteData(pwdstr, ptr, bank, ctr, strData);
            SetPower(PrevPwr);
            if (write)
                return "Tag grabado";
            else
                return "Error al grabar";
        }
        public string WriteTag(string pwdstr, string strData)
        {
            int PrevPwr = GetPower(); //Guardamos la potencia previa, para despues colocarle la que tenia
            SetPower(30); //Seteamos la potencia al maximo
            //bool write = uhfApi.WriteData(pwdstr, bank, ptr, ctr, strData);
            bool write = uhfApi.WriteDataToEpc(pwdstr, strData);
            SetPower(PrevPwr);
            if (write)
                return "Tag grabado";
            else
                return "Error al grabar";
        }
        public UHFTAGInfo ReadTagsFromBuffer()
        {
            UHFTAGInfo tagInfo = uhfApi.ReadTagFromBuffer(); //leemos
            if (tagInfo != null) Beep(); //Si no es nulo, es decir, leyo un tag
            return tagInfo;
        }
        public GenericTag FromChainway(UHFTAGInfo chainwayTag)
        {
            return new GenericTag
            {
                TagID = chainwayTag.EPC,
                MemoryBankData = chainwayTag.Tid,
                HexValue = chainwayTag.Tid,
                MemoryBank = MEMORY_BANK.MemoryBankTid,
                IsReadSuccess = true, // Chainway no siempre trae OpStatus, asumimos éxito
                OpCode = ACCESS_OPERATION_CODE.AccessOperationRead,
                OpStatus = ACCESS_OPERATION_STATUS.AccessSuccess,
                ReadTime = DateTime.Now
            };
        }
        private void Beep()
        {
            try
            {
                Tone ??= new ToneGenerator(Stream.Dtmf, 100);
                Tone.StartTone(Android.Media.Tone.PropBeep);
            }
            catch { }
        }
        public void ReadAndNotify()
        {
            UHFTAGInfo tags = ReadTagsFromBuffer();
            OnTagsRead?.Invoke(tags);
        }
    }
}