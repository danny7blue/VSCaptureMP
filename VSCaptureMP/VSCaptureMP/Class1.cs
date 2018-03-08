﻿/*
 * This file is part of VitalSignsCaptureMP v1.003.
 * Copyright (C) 2017-18 John George K., xeonfusion@users.sourceforge.net

    VitalSignsCaptureMP is free software: you can redistribute it and/or modify
    it under the terms of the GNU Lesser General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    VitalSignsCaptureMP is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU Lesser General Public License for more details.

    You should have received a copy of the GNU Lesser General Public License
    along with VitalSignsCaptureMP.  If not, see <http://www.gnu.org/licenses/>.*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Runtime.InteropServices;
using System.Globalization;

namespace VSCaptureMP
{
    public class MPudpclient:UdpClient
    {
        public IPEndPoint m_remoteIPtarget;
        public List<NumericValResult> m_NumericValList = new List<NumericValResult>();
        public List<string> m_NumValHeaders = new List<string>();
        public StringBuilder m_strbuildvalues = new StringBuilder();
        public StringBuilder m_strbuildheaders = new StringBuilder();
		public List<WaveValResult> m_WaveValResultList = new List<WaveValResult>();
		public StringBuilder m_strbuildwavevalues = new StringBuilder();
        public bool m_transmissionstart = true;
        public string m_strTimestamp;
        public ushort m_actiontype;
        public int m_elementcount = 0;
        public int m_headerelementcount = 0;
        public int m_csvexportset = 1;
        public List<SaSpec> m_SaSpecList = new List<SaSpec>();

        public ScaleRangeSpec16 m_scalerangespec = new ScaleRangeSpec16(); 

        public class NumericValResult
        {
            public string Timestamp;
            public string Relativetimestamp;
            public string PhysioID;
            public string Value;
        }

		public class WaveValResult
		{
			public string Timestamp;
			public string Relativetimestamp;
			public string PhysioID;
			public byte[] Value;
            public ushort SaFlags;
            public byte sample_size;
            public byte significant_bits;
            public ushort array_size;
        }


        //Create a singleton udpclient subclass
        private static volatile MPudpclient MPClient = null;

        public static MPudpclient getInstance
        {

            get
            {
                if (MPClient == null)
                {
                    lock (typeof(MPudpclient))
                        if (MPClient == null)
                        {
                            MPClient = new MPudpclient();
                        }

                }
                return MPClient;
            }

        }


        public MPudpclient()
        {
            MPClient = this;

            m_remoteIPtarget = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 24105);

            MPClient.Client.ReceiveTimeout = 20000;
        }

        public void SendAssociationRequest()
        {
            //MPClient.Send(DataConstants.aarq_msg, DataConstants.aarq_msg.Length);
            MPClient.Send(DataConstants.aarq_msg_ext_poll2, DataConstants.aarq_msg_ext_poll2.Length);
        }

		public void SendWaveAssociationRequest()
		{
			//MPClient.Send(DataConstants.aarq_msg_ext_poll2, DataConstants.aarq_msg_ext_poll2.Length);
			MPClient.Send(DataConstants.aarq_msg_wave_ext_poll2, DataConstants.aarq_msg_wave_ext_poll2.Length);
		}


        public void SendMDSCreateEventResult()
        {
            MPClient.Send(DataConstants.mds_create_resp_msg, DataConstants.mds_create_resp_msg.Length);
        }

        public void SendPollDataRequest()
        {
            MPClient.Send(DataConstants.poll_request_msg, DataConstants.poll_request_msg.Length);
        }

        public void SendExtendedPollDataRequest()
        {
            MPClient.Send(DataConstants.ext_poll_request_msg3, DataConstants.ext_poll_request_msg3.Length);
            //MPClient.Send(DataConstants.ext_poll_request_msg, DataConstants.ext_poll_request_msg.Length);
            
        }

		public void SendExtendedPollWaveDataRequest()
		{
			MPClient.Send(DataConstants.ext_poll_request_wave_msg, DataConstants.ext_poll_request_wave_msg.Length);
			//MPClient.Send(DataConstants.ext_poll_request_msg3, DataConstants.ext_poll_request_msg3.Length)
		}

        public void GetRTSAPriorityListRequest()
        {
            MPClient.Send(DataConstants.get_rtsa_prio_msg, DataConstants.get_rtsa_prio_msg.Length);
        }

        public void SetRTSAPriorityListRequest()
        {
            MPClient.Send(DataConstants.set_rtsa_prio_msg, DataConstants.set_rtsa_prio_msg.Length);
        }

        public void SetRTSAPriorityList(int nWaveSetType)
        {
            List<byte> WaveTrType = new List<byte>();
            CreateWaveformSet(nWaveSetType, WaveTrType);
            SendRTSAPriorityMessage(WaveTrType.ToArray());
        }

        public static void CreateWaveformSet(int nWaveSetType, List<byte> WaveTrtype)
        {
            //Upto 3 ECG and/or 8 non-ECG waveforms can be polled by selecting the appropriate labels
            //in the Wave object priority list

            switch (nWaveSetType)
            {
                case 0:
                    break;
                case 1:
                    WaveTrtype.AddRange(BitConverter.GetBytes(correctendianshortus(0x03))); //count
                    WaveTrtype.AddRange(BitConverter.GetBytes(correctendianshortus(0x0C))); //length
                    WaveTrtype.AddRange(BitConverter.GetBytes(correctendianuint((uint)(Enum.Parse(typeof(DataConstants.WavesIDLabels), "NLS_NOM_ECG_ELEC_POTL_II")))));
                    WaveTrtype.AddRange(BitConverter.GetBytes(correctendianuint((uint)(Enum.Parse(typeof(DataConstants.WavesIDLabels), "NLS_NOM_ECG_ELEC_POTL_I")))));
                    WaveTrtype.AddRange(BitConverter.GetBytes(correctendianuint((uint)(Enum.Parse(typeof(DataConstants.WavesIDLabels), "NLS_NOM_ECG_ELEC_POTL_III")))));
                    break;
                case 2:
                    WaveTrtype.AddRange(BitConverter.GetBytes(correctendianshortus(0x09))); //count
                    WaveTrtype.AddRange(BitConverter.GetBytes(correctendianshortus(0x24))); //length
                    WaveTrtype.AddRange(BitConverter.GetBytes(correctendianuint((uint)(Enum.Parse(typeof(DataConstants.WavesIDLabels), "NLS_NOM_ECG_ELEC_POTL_II")))));
                    WaveTrtype.AddRange(BitConverter.GetBytes(correctendianuint((uint)(Enum.Parse(typeof(DataConstants.WavesIDLabels), "NLS_NOM_ECG_ELEC_POTL_V5")))));
                    WaveTrtype.AddRange(BitConverter.GetBytes(correctendianuint((uint)(Enum.Parse(typeof(DataConstants.WavesIDLabels), "NLS_NOM_RESP")))));
                    WaveTrtype.AddRange(BitConverter.GetBytes(correctendianuint((uint)(Enum.Parse(typeof(DataConstants.WavesIDLabels), "NLS_NOM_PULS_OXIM_PLETH")))));
                    WaveTrtype.AddRange(BitConverter.GetBytes(correctendianuint((uint)(Enum.Parse(typeof(DataConstants.WavesIDLabels), "NLS_NOM_PRESS_BLD_ART")))));
                    WaveTrtype.AddRange(BitConverter.GetBytes(correctendianuint((uint)(Enum.Parse(typeof(DataConstants.WavesIDLabels), "NLS_NOM_PRESS_BLD_VEN_CENT")))));
                    WaveTrtype.AddRange(BitConverter.GetBytes(correctendianuint((uint)(Enum.Parse(typeof(DataConstants.WavesIDLabels), "NLS_NOM_AWAY_CO2")))));
                    WaveTrtype.AddRange(BitConverter.GetBytes(correctendianuint((uint)(Enum.Parse(typeof(DataConstants.WavesIDLabels), "NLS_NOM_PRESS_AWAY")))));
                    WaveTrtype.AddRange(BitConverter.GetBytes(correctendianuint((uint)(Enum.Parse(typeof(DataConstants.WavesIDLabels), "NLS_NOM_FLOW_AWAY")))));
                    break;
                case 3:
                    WaveTrtype.AddRange(BitConverter.GetBytes(correctendianshortus(0x03))); //count
                    WaveTrtype.AddRange(BitConverter.GetBytes(correctendianshortus(0x0C))); //length
                    WaveTrtype.AddRange(BitConverter.GetBytes(correctendianuint((uint)(Enum.Parse(typeof(DataConstants.WavesIDLabels), "NLS_NOM_ECG_ELEC_POTL_AVR")))));
                    WaveTrtype.AddRange(BitConverter.GetBytes(correctendianuint((uint)(Enum.Parse(typeof(DataConstants.WavesIDLabels), "NLS_NOM_ECG_ELEC_POTL_AVL")))));
                    WaveTrtype.AddRange(BitConverter.GetBytes(correctendianuint((uint)(Enum.Parse(typeof(DataConstants.WavesIDLabels), "NLS_NOM_ECG_ELEC_POTL_AVF")))));
                    break;
                case 4:
                    WaveTrtype.AddRange(BitConverter.GetBytes(correctendianshortus(0x03))); //count
                    WaveTrtype.AddRange(BitConverter.GetBytes(correctendianshortus(0x0C))); //length
                    WaveTrtype.AddRange(BitConverter.GetBytes(correctendianuint((uint)(Enum.Parse(typeof(DataConstants.WavesIDLabels), "NLS_NOM_ECG_ELEC_POTL_V1")))));
                    WaveTrtype.AddRange(BitConverter.GetBytes(correctendianuint((uint)(Enum.Parse(typeof(DataConstants.WavesIDLabels), "NLS_NOM_ECG_ELEC_POTL_V2")))));
                    WaveTrtype.AddRange(BitConverter.GetBytes(correctendianuint((uint)(Enum.Parse(typeof(DataConstants.WavesIDLabels), "NLS_NOM_ECG_ELEC_POTL_V3")))));
                    break;
                case 5:
                    WaveTrtype.AddRange(BitConverter.GetBytes(correctendianshortus(0x03))); //count
                    WaveTrtype.AddRange(BitConverter.GetBytes(correctendianshortus(0x0C))); //length
                    WaveTrtype.AddRange(BitConverter.GetBytes(correctendianuint((uint)(Enum.Parse(typeof(DataConstants.WavesIDLabels), "NLS_NOM_ECG_ELEC_POTL_V4")))));
                    WaveTrtype.AddRange(BitConverter.GetBytes(correctendianuint((uint)(Enum.Parse(typeof(DataConstants.WavesIDLabels), "NLS_NOM_ECG_ELEC_POTL_V5")))));
                    WaveTrtype.AddRange(BitConverter.GetBytes(correctendianuint((uint)(Enum.Parse(typeof(DataConstants.WavesIDLabels), "NLS_NOM_ECG_ELEC_POTL_V6")))));
                    break;
                case 6:
                    WaveTrtype.AddRange(BitConverter.GetBytes(correctendianshortus(0x04))); //count
                    WaveTrtype.AddRange(BitConverter.GetBytes(correctendianshortus(0x10))); //length
                    WaveTrtype.AddRange(BitConverter.GetBytes(correctendianuint((uint)(Enum.Parse(typeof(DataConstants.WavesIDLabels), "NLS_EEG_NAMES_EEG_CHAN1_LBL")))));
                    WaveTrtype.AddRange(BitConverter.GetBytes(correctendianuint((uint)(Enum.Parse(typeof(DataConstants.WavesIDLabels), "NLS_EEG_NAMES_EEG_CHAN2_LBL")))));
                    WaveTrtype.AddRange(BitConverter.GetBytes(correctendianuint((uint)(Enum.Parse(typeof(DataConstants.WavesIDLabels), "NLS_EEG_NAMES_EEG_CHAN3_LBL")))));
                    WaveTrtype.AddRange(BitConverter.GetBytes(correctendianuint((uint)(Enum.Parse(typeof(DataConstants.WavesIDLabels), "NLS_EEG_NAMES_EEG_CHAN4_LBL")))));
                    break;
                case 7:
                    WaveTrtype.AddRange(BitConverter.GetBytes(correctendianshortus(0x01))); //count
                    WaveTrtype.AddRange(BitConverter.GetBytes(correctendianshortus(0x04))); //length
                    WaveTrtype.AddRange(BitConverter.GetBytes(correctendianuint((uint)(Enum.Parse(typeof(DataConstants.WavesIDLabels), "NLS_NOM_ECG_ELEC_POTL")))));
                    break;
                case 8:
                    WaveTrtype.AddRange(BitConverter.GetBytes(correctendianshortus(0x05))); //count
                    WaveTrtype.AddRange(BitConverter.GetBytes(correctendianshortus(0x14))); //length
                    WaveTrtype.AddRange(BitConverter.GetBytes(correctendianuint((uint)(Enum.Parse(typeof(DataConstants.WavesIDLabels), "NLS_NOM_ECG_ELEC_POTL")))));
                    WaveTrtype.AddRange(BitConverter.GetBytes(correctendianuint((uint)(Enum.Parse(typeof(DataConstants.WavesIDLabels), "NLS_NOM_PULS_OXIM_PLETH")))));
                    WaveTrtype.AddRange(BitConverter.GetBytes(correctendianuint((uint)(Enum.Parse(typeof(DataConstants.WavesIDLabels), "NLS_NOM_PRESS_BLD_ART")))));
                    WaveTrtype.AddRange(BitConverter.GetBytes(correctendianuint((uint)(Enum.Parse(typeof(DataConstants.WavesIDLabels), "NLS_NOM_PRESS_BLD_VEN_CENT")))));
                    WaveTrtype.AddRange(BitConverter.GetBytes(correctendianuint((uint)(Enum.Parse(typeof(DataConstants.WavesIDLabels), "NLS_NOM_AWAY_CO2")))));
                    break;
            }
        }

        public void SendRTSAPriorityMessage(byte[] WaveTrType)
        {
            List<byte> tempbufflist = new List<byte>();

            //Assemble request in reverse order first to calculate lengths
            //Insert TextIdList
            tempbufflist.InsertRange(0, WaveTrType);

            Ava avatype = new Ava();
            avatype.attribute_id = (ushort)IntelliVue.AttributeIDs.NOM_ATTR_POLL_RTSA_PRIO_LIST;
            avatype.length = (ushort)WaveTrType.Length;
            //avatype.length = (ushort)tempbufflist.Count;
            tempbufflist.InsertRange(0, BitConverter.GetBytes(correctendianshortus(avatype.length)));
            tempbufflist.InsertRange(0, BitConverter.GetBytes(correctendianshortus(avatype.attribute_id)));

            byte[] AttributeModEntry = { 0x00, 0x00 };
            tempbufflist.InsertRange(0, AttributeModEntry);

            byte[] ModListlength = BitConverter.GetBytes(correctendianshortus((ushort)tempbufflist.Count));
            byte[] ModListCount = { 0x00, 0x01 };
            tempbufflist.InsertRange(0, ModListlength);
            tempbufflist.InsertRange(0, ModListCount);

            byte[] ManagedObjectID = { 0x00, 0x21, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            tempbufflist.InsertRange(0, ManagedObjectID);

            ROIVapdu rovi = new ROIVapdu();
            rovi.length = (ushort)tempbufflist.Count;
            rovi.command_type = (ushort)IntelliVue.Commands.CMD_CONFIRMED_SET;
            rovi.inovke_id = 0x0000;
            tempbufflist.InsertRange(0, BitConverter.GetBytes(correctendianshortus(rovi.length)));
            tempbufflist.InsertRange(0, BitConverter.GetBytes(correctendianshortus(rovi.command_type)));
            tempbufflist.InsertRange(0, BitConverter.GetBytes(correctendianshortus(rovi.inovke_id)));

            ROapdus roap = new ROapdus();
            roap.length = (ushort)tempbufflist.Count;
            roap.ro_type = (ushort)IntelliVue.RemoteOperationHeader.ROIV_APDU;
            tempbufflist.InsertRange(0, BitConverter.GetBytes(correctendianshortus(roap.length)));
            tempbufflist.InsertRange(0, BitConverter.GetBytes(correctendianshortus(roap.ro_type)));

            byte[] Spdu = { 0xE1, 0x00, 0x00, 0x02 };
            tempbufflist.InsertRange(0, Spdu);

            byte[] finaltxbuff = tempbufflist.ToArray();

            MPClient.Send(finaltxbuff, finaltxbuff.Length);
        }

        public async Task SendCycledExtendedPollWaveDataRequest(int nInterval)
        {
            int nmillisecond = nInterval * 1000;
            if (nmillisecond != 0)
            {
                do
                {
                    MPClient.Send(DataConstants.ext_poll_request_wave_msg, DataConstants.ext_poll_request_wave_msg.Length);
                    await Task.Delay(nmillisecond);

                }
                while (true);
            }
            else MPClient.Send(DataConstants.ext_poll_request_wave_msg, DataConstants.ext_poll_request_wave_msg.Length);
        }


        public async Task SendCycledExtendedPollDataRequest(int nInterval)
        {
            int nmillisecond = nInterval * 1000;
            if (nmillisecond != 0)
            { 
                do
                {
                    MPClient.Send(DataConstants.ext_poll_request_msg, DataConstants.ext_poll_request_msg.Length);
                    await Task.Delay(nmillisecond);

                }
                while (true);
            }
            else MPClient.Send(DataConstants.ext_poll_request_msg, DataConstants.ext_poll_request_msg.Length);
        }

        public async Task KeepConnectionAlive(int nInterval)
        {
            //int nmillisecond = (nInterval/2) * 1000;
            int nmillisecond = 6 * 1000;
            if (nmillisecond != 0 && nInterval !=1)
            {
                do
                {
                    SendMDSCreateEventResult();
                    await Task.Delay(nmillisecond);
                }
                while (true);
            }
            
        }
        
        public void ParseMDSCreateEventReport (byte[] readmdsconnectbuffer)
        {
            MemoryStream memstream = new MemoryStream(readmdsconnectbuffer);
            BinaryReader binreader = new BinaryReader(memstream);

            byte[] header = binreader.ReadBytes(34);
            ushort attriblist_count = correctendianshortus(binreader.ReadUInt16());
            ushort attriblist_length = correctendianshortus(binreader.ReadUInt16());
            int avaobjectscount = Convert.ToInt32(attriblist_count);

            if (avaobjectscount > 0)
            {
                byte[] attriblistobjects = binreader.ReadBytes(attriblist_length);

                MemoryStream memstream2 = new MemoryStream(attriblistobjects);
                BinaryReader binreader2 = new BinaryReader(memstream2);


                for (int i = 0; i < avaobjectscount; i++)
                {

                    Ava avaobjects = new Ava();
                    DecodeMDSAttribObjects(ref avaobjects, ref binreader2);
                }
            }

        }

        public void DecodeMDSAttribObjects(ref Ava avaobject, ref BinaryReader binreader)
        {
            avaobject.attribute_id = correctendianshortus(binreader.ReadUInt16());
            avaobject.length = correctendianshortus(binreader.ReadUInt16());
            //avaobject.attribute_val = correctendianshortus(binreader4.ReadUInt16());
            if (avaobject.length > 0)
            {
                byte[] avaattribobjects = binreader.ReadBytes(avaobject.length);


                switch (avaobject.attribute_id)
                {
                    //Get Date and Time
                    case DataConstants.NOM_ATTR_TIME_ABS:
                        break;
                    //Get Relative Time attribute
                    case DataConstants.NOM_ATTR_TIME_REL:
                        break;
                    //Get Patient demographics
                    case DataConstants.NOM_ATTR_PT_ID:
                        break;
                    case DataConstants.NOM_ATTR_PT_NAME_GIVEN:
                        break;
                    case DataConstants.NOM_ATTR_PT_NAME_FAMILY:
                        break;
                    case DataConstants.NOM_ATTR_PT_DOB:
                        break;
                }
            }


        }

        public void ReadData(byte[] readbuffer)
        {
            ProcessPacket(readbuffer);
        }

        public void ProcessPacket(byte[] packetbuffer)
        {
            MemoryStream memstream = new MemoryStream(packetbuffer);
            BinaryReader binreader = new BinaryReader(memstream);

            byte[] sessionheader = binreader.ReadBytes(4);
            ushort ROapdu_type = correctendianshortus(binreader.ReadUInt16());

            switch(ROapdu_type)
            {
                case DataConstants.ROIV_APDU:
                    // This is an MDS create event, answer with create response
                    ParseMDSCreateEventReport(packetbuffer);
                    SendMDSCreateEventResult();
                    break;
                case DataConstants.RORS_APDU:
                    CheckPollPacketActionType(packetbuffer);
                    break;
                case DataConstants.RORLS_APDU:
                    CheckLinkedPollPacketActionType(packetbuffer);
                    break;
                case DataConstants.ROER_APDU:
                    break;
                default:
                    break;
            }
            
        }

        public void CheckPollPacketActionType(byte[] packetbuffer)
        {
            MemoryStream memstream = new MemoryStream(packetbuffer);
            BinaryReader binreader = new BinaryReader(memstream);

            byte[] header = binreader.ReadBytes(20);
            ushort action_type = correctendianshortus(binreader.ReadUInt16());
            m_actiontype = action_type;

            switch (action_type)
            {
                case DataConstants.NOM_ACT_POLL_MDIB_DATA:
                    PollPacketDecoder(packetbuffer, 44);
                    break;
                case DataConstants.NOM_ACT_POLL_MDIB_DATA_EXT:
                    PollPacketDecoder(packetbuffer, 46);
                    break;
                default:
                    break;
            }

        }

        public void CheckLinkedPollPacketActionType( byte[] packetbuffer)
        {
            MemoryStream memstream = new MemoryStream(packetbuffer);
            BinaryReader binreader = new BinaryReader(memstream);

            byte[] header = binreader.ReadBytes(22);
            ushort action_type = correctendianshortus(binreader.ReadUInt16());
            m_actiontype = action_type;

            switch(action_type)
            {
                case DataConstants.NOM_ACT_POLL_MDIB_DATA:
                    PollPacketDecoder(packetbuffer, 46);
                    break;
                case DataConstants.NOM_ACT_POLL_MDIB_DATA_EXT:
                    PollPacketDecoder(packetbuffer, 48);
                    break;
                default:
                    break;
            }

        }

        public void PollPacketDecoder(byte[] packetbuffer, int headersize)
        {
            int packetsize = packetbuffer.GetLength(0);

            MemoryStream memstream = new MemoryStream(packetbuffer);
            BinaryReader binreader = new BinaryReader(memstream);

            byte[] header = binreader.ReadBytes(headersize);
            byte[] packetdata = new byte[packetsize - header.Length];
            Array.Copy(packetbuffer, header.Length, packetdata, 0, packetdata.Length);

            m_strTimestamp = GetPacketTimestamp(header);
            DateTime dtDateTime = DateTime.Now;
            string strDateTime = dtDateTime.ToString("dd-MM-yyyy HH:mm:ss.fff", CultureInfo.InvariantCulture);
            Console.WriteLine("Time:{0}", strDateTime);
            Console.WriteLine("Time:{0}", m_strTimestamp);


            //ParsePacketType

            PollInfoList pollobjects = new PollInfoList();
            
            int scpollobjectscount = DecodePollObjects(ref pollobjects, packetdata);

            if (scpollobjectscount > 0)
            {
                MemoryStream memstream2 = new MemoryStream(pollobjects.scpollarray);
                BinaryReader binreader2 = new BinaryReader(memstream2);


                for (int i = 0; i < scpollobjectscount; i++)
                {

                    SingleContextPoll scpoll = new SingleContextPoll();
                    int obpollobjectscount = DecodeSingleContextPollObjects(ref scpoll, ref binreader2);

                    if (obpollobjectscount > 0)
                    {
                        MemoryStream memstream3 = new MemoryStream(scpoll.obpollobjectsarray);
                        BinaryReader binreader3 = new BinaryReader(memstream3);

                        for (int j = 0; j < obpollobjectscount; j++)
                        {

                            ObservationPoll obpollobject = new ObservationPoll();
                            int avaobjectscount = DecodeObservationPollObjects(ref obpollobject, ref binreader3);

                            if (avaobjectscount > 0)
                            {
                                MemoryStream memstream4 = new MemoryStream(obpollobject.avaobjectsarray);
                                BinaryReader binreader4 = new BinaryReader(memstream4);

                                for (int k = 0; k < avaobjectscount; k++)
                                {
                                    Ava avaobject = new Ava();
                                    DecodeAvaObjects(ref avaobject, ref binreader4);
                                }
                                
                            }
                        }
                    }
                }

                ExportDataToCSV();
				ExportWaveToCSV();
            }
            
        }

        public int DecodePollObjects(ref PollInfoList pollobjects, byte[] packetbuffer)
        {

            MemoryStream memstream = new MemoryStream(packetbuffer);
            BinaryReader binreader = new BinaryReader(memstream);

            pollobjects.count = correctendianshortus(binreader.ReadUInt16());
            if(pollobjects.count>0) pollobjects.length = correctendianshortus(binreader.ReadUInt16());

            int scpollobjectscount = Convert.ToInt32(pollobjects.count);
            if(pollobjects.length>0) pollobjects.scpollarray = binreader.ReadBytes(pollobjects.length);

            return scpollobjectscount;
        }

        public int DecodeSingleContextPollObjects(ref SingleContextPoll scpoll, ref BinaryReader binreader2)
        {
            scpoll.context_id = correctendianshortus(binreader2.ReadUInt16());
            scpoll.count = correctendianshortus(binreader2.ReadUInt16());
            //There can be empty singlecontextpollobjects
            //if(scpoll.count>0) scpoll.length = correctendianshortus(binreader2.ReadUInt16());
            scpoll.length = correctendianshortus(binreader2.ReadUInt16());
            
            int obpollobjectscount = Convert.ToInt32(scpoll.count);
            if(scpoll.length>0) scpoll.obpollobjectsarray = binreader2.ReadBytes(scpoll.length);

            return obpollobjectscount;
        }

        public int DecodeObservationPollObjects(ref ObservationPoll obpollobject, ref BinaryReader binreader3)
        {
            obpollobject.obj_handle = correctendianshortus(binreader3.ReadUInt16());

            AttributeList attributeliststruct = new AttributeList();

            attributeliststruct.count = correctendianshortus(binreader3.ReadUInt16());
            if(attributeliststruct.count>0) attributeliststruct.length = correctendianshortus(binreader3.ReadUInt16());

            int avaobjectscount = Convert.ToInt32(attributeliststruct.count);
            if(attributeliststruct.length>0) obpollobject.avaobjectsarray = binreader3.ReadBytes(attributeliststruct.length);

            return avaobjectscount;
        }

        public void DecodeAvaObjects(ref Ava avaobject, ref BinaryReader binreader4)
        {
            avaobject.attribute_id = correctendianshortus(binreader4.ReadUInt16());
            avaobject.length = correctendianshortus(binreader4.ReadUInt16());
            //avaobject.attribute_val = correctendianshortus(binreader4.ReadUInt16());
            if (avaobject.length > 0)
            {
                byte[] avaattribobjects = binreader4.ReadBytes(avaobject.length);


                switch (avaobject.attribute_id)
                {
                    case DataConstants.NOM_ATTR_ID_LABEL:
                        break;
                    case DataConstants.NOM_ATTR_NU_VAL_OBS:
                        ReadNumericObservationValue(avaattribobjects);
                        break;
                    case DataConstants.NOM_ATTR_NU_CMPD_VAL_OBS:
                        ReadCompoundNumericObsValue(avaattribobjects);
                        break;
                    case DataConstants.NOM_ATTR_METRIC_SPECN:
                        break;
                    case DataConstants.NOM_ATTR_ID_LABEL_STRING:
                        ReadIDLabelString(avaattribobjects);
                        break;
					case DataConstants.NOM_ATTR_SA_VAL_OBS:
						ReadWaveSaObservationValueObject(avaattribobjects);
						break;
					case DataConstants.NOM_ATTR_SA_CMPD_VAL_OBS:
						ReadCompoundWaveSaObservationValue(avaattribobjects);
						break;
                    case DataConstants.NOM_ATTR_SA_SPECN:
                        ReadSaSpecifications(avaattribobjects);
                        break;
                    case DataConstants.NOM_ATTR_SCALE_SPECN_I16:
                        ReadSaScaleSpecifications(avaattribobjects);
                        break;
                    default:
                        // unknown attribute -> do nothing
                        break;

                }
            }

        }

        public string GetPacketTimestamp(byte[] header)
        {
            MemoryStream memstream = new MemoryStream(header);
            BinaryReader binreader = new BinaryReader(memstream);

            int pollmdibdatareplysize = 20;
            if (m_actiontype == DataConstants.NOM_ACT_POLL_MDIB_DATA) pollmdibdatareplysize = 20;
            else if (m_actiontype == DataConstants.NOM_ACT_POLL_MDIB_DATA_EXT) pollmdibdatareplysize = 22;

            int firstpartheaderlength = (header.Length - pollmdibdatareplysize);
            byte[] firstpartheader = binreader.ReadBytes(firstpartheaderlength);
            byte[] pollmdibdatareplyarray = binreader.ReadBytes(pollmdibdatareplysize);

            double relativetime =0;
            if (m_actiontype == DataConstants.NOM_ACT_POLL_MDIB_DATA)
            { 
                PollMdibDataReply pollmdibdatareply = new PollMdibDataReply();

                MemoryStream memstream2 = new MemoryStream(pollmdibdatareplyarray);
                BinaryReader binreader2 = new BinaryReader(memstream2);

                pollmdibdatareply.poll_number = correctendianshortus(binreader2.ReadUInt16());
                pollmdibdatareply.rel_time_stamp = correctendianuint(binreader2.ReadUInt32());

                relativetime = pollmdibdatareply.rel_time_stamp;
            }
            else if(m_actiontype == DataConstants.NOM_ACT_POLL_MDIB_DATA_EXT)
            {
                PollMdibDataReplyExt pollmdibdatareplyext = new PollMdibDataReplyExt();

                MemoryStream memstream2 = new MemoryStream(pollmdibdatareplyarray);
                BinaryReader binreader2 = new BinaryReader(memstream2);

                pollmdibdatareplyext.poll_number = correctendianshortus(binreader2.ReadUInt16());
                pollmdibdatareplyext.sequence_no = correctendianshortus(binreader2.ReadUInt16());
                pollmdibdatareplyext.rel_time_stamp = correctendianuint(binreader2.ReadUInt32());

                relativetime = pollmdibdatareplyext.rel_time_stamp;
            }
            
            string strRelativeTime = relativetime.ToString();
            
            //AbsoluteTime is not supported by several monitors
            /*AbsoluteTime absolutetime = new AbsoluteTime();

            absolutetime.century = binreader2.ReadByte();
            absolutetime.year = binreader2.ReadByte();
            absolutetime.month = binreader2.ReadByte();
            absolutetime.day = binreader2.ReadByte();
            absolutetime.hour = binreader2.ReadByte();
            absolutetime.minute = binreader2.ReadByte();
            absolutetime.second = binreader2.ReadByte();
            absolutetime.fraction = binreader2.ReadByte();*/

            return strRelativeTime;

        }

        public void ReadIDLabelString(byte[] avaattribobjects)
        {

            MemoryStream memstream5 = new MemoryStream(avaattribobjects);
            BinaryReader binreader5 = new BinaryReader(memstream5);

            StringMP strmp = new StringMP();

            strmp.length = correctendianshortus(binreader5.ReadUInt16());
            //strmp.value1 = correctendianshortus(binreader5.ReadUInt16());
            byte[] stringval = binreader5.ReadBytes(strmp.length);

            string label = Encoding.UTF8.GetString(stringval);
            Console.WriteLine("Label String: {0}", label);

        }

        public void ReadNumericObservationValue(byte[] avaattribobjects)
        {
            MemoryStream memstream5 = new MemoryStream(avaattribobjects);
            BinaryReader binreader5 = new BinaryReader(memstream5);

            NuObsValue NumObjectValue = new NuObsValue();
            NumObjectValue.physio_id = correctendianshortus(binreader5.ReadUInt16());
            NumObjectValue.state = correctendianshortus(binreader5.ReadUInt16());
            NumObjectValue.unit_code = correctendianshortus(binreader5.ReadUInt16());
            NumObjectValue.value = correctendianuint(binreader5.ReadUInt32());

            double value = FloattypeToValue(NumObjectValue.value);

            //string physio_id = NumObjectValue.physio_id.ToString();
            string physio_id = Enum.GetName(typeof(IntelliVue.AlertSource), NumObjectValue.physio_id);

            string state = NumObjectValue.state.ToString();
            string unit_code = NumObjectValue.unit_code.ToString();

            string valuestr;
            if (value != DataConstants.FLOATTYPE_NAN)
            {
                valuestr = value.ToString();
            }
            else valuestr = "-";

            NumericValResult NumVal = new NumericValResult();
            NumVal.Relativetimestamp = m_strTimestamp;
            NumVal.Timestamp = DateTime.Now.ToString();
            NumVal.PhysioID = physio_id;
            NumVal.Value = valuestr;


            m_NumericValList.Add(NumVal);
            m_NumValHeaders.Add(NumVal.PhysioID);

            Console.WriteLine("Physiological ID: {0}", physio_id);
            //Console.WriteLine("State: {0}", state);
            //Console.WriteLine("Unit code: {0}", unit_code);
            Console.WriteLine("Value: {0}", valuestr);
            Console.WriteLine();
        }

        public void ReadCompoundNumericObsValue(byte[] avaattribobjects)
        {
            MemoryStream memstream6 = new MemoryStream(avaattribobjects);
            BinaryReader binreader6 = new BinaryReader(memstream6);

            NuObsValueCmp NumObjectValueCmp = new NuObsValueCmp();
            NumObjectValueCmp.count = correctendianshortus(binreader6.ReadUInt16());
            NumObjectValueCmp.length = correctendianshortus(binreader6.ReadUInt16());

            int cmpnumericobjectscount = Convert.ToInt32(NumObjectValueCmp.count);

            if(cmpnumericobjectscount>0)
            {
                for (int j = 0; j < cmpnumericobjectscount; j++)
                {
                    byte[] cmpnumericarrayobject = binreader6.ReadBytes(10);

                    ReadNumericObservationValue(cmpnumericarrayobject);
                }
            }

        }

		public void ReadWaveSaObservationValueObject(byte[] avaattribobjects)
		{
			MemoryStream memstream7 = new MemoryStream(avaattribobjects);
			BinaryReader binreader7 = new BinaryReader(memstream7);

			ReadWaveSaObservationValue(ref binreader7);

		}

        public void ReadSaSpecifications(byte[] avaattribobjects)
        {
            MemoryStream memstream7 = new MemoryStream(avaattribobjects);
            BinaryReader binreader7 = new BinaryReader(memstream7);

            SaSpec Saspecobj = new SaSpec();
            Saspecobj.array_size = correctendianshortus(binreader7.ReadUInt16());
            Saspecobj.sample_size = binreader7.ReadByte();
            Saspecobj.significant_bits = binreader7.ReadByte();
            Saspecobj.SaFlags = correctendianshortus(binreader7.ReadUInt16());

            //Add to a list of Sample array specification definitions if it's not already present
            if (!m_SaSpecList.Exists(x => x.array_size == Saspecobj.array_size))
            {
                m_SaSpecList.Add(Saspecobj);
            }

        }

        public void ReadWaveSaObservationValue(ref BinaryReader binreader7)
		{ 
			SaObsValue WaveSaObjectValue = new SaObsValue();
			WaveSaObjectValue.physio_id = correctendianshortus(binreader7.ReadUInt16());
            WaveSaObjectValue.state = correctendianshortus(binreader7.ReadUInt16());
            WaveSaObjectValue.length = correctendianshortus(binreader7.ReadUInt16());

			int wavevalobjectslength = Convert.ToInt32(WaveSaObjectValue.length);
			byte[] WaveValObjects = binreader7.ReadBytes(wavevalobjectslength);

			string physio_id = Enum.GetName(typeof(IntelliVue.AlertSource), WaveSaObjectValue.physio_id);

			WaveValResult WaveVal = new WaveValResult();
			WaveVal.Relativetimestamp = m_strTimestamp;

            DateTime dtDateTime = DateTime.Now;
            string strDateTime = dtDateTime.ToString("dd-MM-yyyy HH:mm:ss.fff", CultureInfo.InvariantCulture);
            //WaveVal.Timestamp = DateTime.Now.ToString();
            WaveVal.Timestamp = strDateTime;
            WaveVal.PhysioID = physio_id;

			WaveVal.Value = new byte[wavevalobjectslength];
			Array.Copy(WaveValObjects, WaveVal.Value, wavevalobjectslength);

            //Find the Sample array specification definition that matches the observation sample array size
            SaSpec saspecobj = new SaSpec();
            if (wavevalobjectslength % 128 == 0)
            {
                saspecobj = m_SaSpecList.Find(x => x.array_size == 128);
                if (saspecobj == null)
                {
                    //use default values for ecg
                    WaveVal.significant_bits = 0x0E;
                    WaveVal.SaFlags = 0x3000;
                    WaveVal.sample_size = 0x10;
                    WaveVal.array_size = 0x80;
                }

            }
            else if (wavevalobjectslength % 64 == 0)
            {
                saspecobj = m_SaSpecList.Find(x => x.array_size == 64);
                if (saspecobj == null)
                {
                    //use default values for ecg
                    WaveVal.significant_bits = 0x0E;
                    WaveVal.SaFlags = 0x3000;
                    WaveVal.sample_size = 0x10;
                    WaveVal.array_size = 0x40;
                }

            }
            else if (wavevalobjectslength % 32 == 0)
            {
                saspecobj = m_SaSpecList.Find(x => x.array_size == 32);
                if (saspecobj == null)
                {
                    //use default values for resp
                    WaveVal.significant_bits = 0x0C;
                    WaveVal.SaFlags = 0x8000;
                    WaveVal.sample_size = 0x10;
                    WaveVal.array_size = 0x20;
                }

            }
            else if (wavevalobjectslength % 16 == 0)
            {
                saspecobj = m_SaSpecList.Find(x => x.array_size == 16);
                if (saspecobj == null)
                {
                    //use default values for pleth
                    WaveVal.significant_bits = 0x0C;
                    WaveVal.SaFlags = 0x8000;
                    WaveVal.sample_size = 0x10;
                    WaveVal.array_size = 0x10;
                }
            }

            if (saspecobj != null)
            {
                WaveVal.significant_bits = saspecobj.significant_bits;
                WaveVal.SaFlags = saspecobj.SaFlags;
                WaveVal.sample_size = saspecobj.sample_size;
                WaveVal.array_size = saspecobj.array_size;
            }

            m_WaveValResultList.Add(WaveVal);
	
		}

		public void ReadCompoundWaveSaObservationValue(byte[] avaattribobjects)
		{
			MemoryStream memstream8 = new MemoryStream(avaattribobjects);
			BinaryReader binreader8 = new BinaryReader(memstream8);

			SaObsValueCmp WaveSaObjectValueCmp = new SaObsValueCmp();
			WaveSaObjectValueCmp.count = correctendianshortus(binreader8.ReadUInt16());
            WaveSaObjectValueCmp.length = correctendianshortus(binreader8.ReadUInt16());

            int cmpwaveobjectscount = Convert.ToInt32(WaveSaObjectValueCmp.count);
			int cmpwaveobjectslength = Convert.ToInt32(WaveSaObjectValueCmp.length);

			byte[] cmpwavearrayobject = binreader8.ReadBytes(cmpwaveobjectslength);

            if(cmpwaveobjectscount>0)
            {
				MemoryStream memstream9 = new MemoryStream(cmpwavearrayobject);
				BinaryReader binreader9 = new BinaryReader(memstream9);

				for (int k = 0; k<cmpwaveobjectscount; k++)
                {
					ReadWaveSaObservationValue(ref binreader9);
                }
            }
		}

        public void ReadSaScaleSpecifications(byte[] avaattribobjects)
        {
            MemoryStream memstream9 = new MemoryStream(avaattribobjects);
            BinaryReader binreader9 = new BinaryReader(memstream9);

            ScaleRangeSpec16 ScaleSpec = new ScaleRangeSpec16();

            ScaleSpec.lower_absolute_value = FloattypeToValue( correctendianuint(binreader9.ReadUInt32()) );
            ScaleSpec.upper_absolute_value = FloattypeToValue( correctendianuint(binreader9.ReadUInt32()) );
            ScaleSpec.lower_scaled_value = correctendianshortus(binreader9.ReadUInt16());
            ScaleSpec.upper_scaled_value = correctendianshortus(binreader9.ReadUInt16());

            m_scalerangespec = ScaleSpec;
        }

        public static double FloattypeToValue(uint fvalue)
        {
            double value = 0;
            if (fvalue != DataConstants.FLOATTYPE_NAN)
            {
                uint exponentbits = (fvalue >> 24);
                uint mantissabits = (fvalue << 8);
                mantissabits = (mantissabits >> 8);

                sbyte signedexponentbits = (sbyte)exponentbits; // Get Two's complement signed byte
                decimal exponent = Convert.ToDecimal(signedexponentbits);

                double mantissa = mantissabits;
                value = mantissa * Math.Pow((double)10, (double)exponent);

                return value;
            }
            else return (double)fvalue;
        }
        
        public static int correctendianshort(ushort sValue)
        {
            byte [] bArray = BitConverter.GetBytes(sValue);
            if (BitConverter.IsLittleEndian) Array.Reverse(bArray);

            int nresult = BitConverter.ToInt16(bArray, 0);
            return nresult;
        }

        public static ushort correctendianshortus(ushort sValue)
        {
            byte[] bArray = BitConverter.GetBytes(sValue);
            if (BitConverter.IsLittleEndian) Array.Reverse(bArray);

            ushort result = BitConverter.ToUInt16(bArray, 0);
            return result;
        }

        public static uint correctendianuint(uint sValue)
        {
            byte[] bArray = BitConverter.GetBytes(sValue);
            if (BitConverter.IsLittleEndian) Array.Reverse(bArray);

            uint result = BitConverter.ToUInt32(bArray, 0);
            return result;
        }

        public static short correctendianshorts(short sValue)
        {
            byte[] bArray = BitConverter.GetBytes(sValue);
            if (BitConverter.IsLittleEndian) Array.Reverse(bArray);

            short result = BitConverter.ToInt16(bArray, 0);
            return result;
        }

        public void ExportDataToCSV()
        {
            switch(m_csvexportset)
            {
                case 1:
                    SaveNumericValueList();
                    break;
                case 2:
                    SaveNumericValueListRows();
                    break;
                case 3:
                    SaveNumericValueListConsolidatedCSV();
                    break;
                default:
                    break;
            }
        }

        public void WriteNumericHeadersList()
        {
            if (m_NumericValList.Count != 0)
            {
                string pathcsv = Path.Combine(Directory.GetCurrentDirectory(), "MPDataExport.csv");

                m_strbuildheaders.Append("Time");
                m_strbuildheaders.Append(',');
                m_strbuildheaders.Append("RelativeTime");
                m_strbuildheaders.Append(',');


                foreach (NumericValResult NumValResult in m_NumericValList)
                {
                    m_strbuildheaders.Append(NumValResult.PhysioID);
                    m_strbuildheaders.Append(',');

                }

                m_strbuildheaders.Remove(m_strbuildheaders.Length - 1, 1);
                m_strbuildheaders.Replace(",,", ",");
                m_strbuildheaders.AppendLine();
                ExportNumValListToCSVFile(pathcsv, m_strbuildheaders);
                    
                m_strbuildheaders.Clear();
                m_NumValHeaders.RemoveRange(0, m_NumValHeaders.Count);


            }
        }

        public void SaveNumericValueList()
        {
            if (m_NumericValList.Count != 0)
            {
                string pathcsv = Path.Combine(Directory.GetCurrentDirectory(), "MPDataExport.csv");

                foreach (NumericValResult NumValResult in m_NumericValList)
                {
                    m_strbuildvalues.Append(NumValResult.Timestamp);
                    m_strbuildvalues.Append(',');
                    m_strbuildvalues.Append(NumValResult.Relativetimestamp);
                    m_strbuildvalues.Append(',');
                    m_strbuildvalues.Append(NumValResult.PhysioID);
                    m_strbuildvalues.Append(',');
                    m_strbuildvalues.Append(NumValResult.Value);
                    m_strbuildvalues.AppendLine();
                }

                ExportNumValListToCSVFile(pathcsv, m_strbuildvalues);
                m_strbuildvalues.Clear();
                m_NumericValList.RemoveRange(0,m_NumericValList.Count);
            }
        }

        public void SaveNumericValueListRows()
        {
            if (m_NumericValList.Count != 0)
            {
                WriteNumericHeadersList();
                string pathcsv = Path.Combine(Directory.GetCurrentDirectory(), "MPDataExport.csv");

                m_strbuildvalues.Append(m_NumericValList.ElementAt(0).Timestamp);
                m_strbuildvalues.Append(',');
                m_strbuildvalues.Append(m_NumericValList.ElementAt(0).Relativetimestamp);
                m_strbuildvalues.Append(',');


                foreach (NumericValResult NumValResult in m_NumericValList)
                {
                    m_strbuildvalues.Append(NumValResult.Value);
                    m_strbuildvalues.Append(',');
                   
                }

                m_strbuildvalues.Remove(m_strbuildvalues.Length - 1, 1);
                m_strbuildvalues.Replace(",,", ",");
                m_strbuildvalues.AppendLine();

                ExportNumValListToCSVFile(pathcsv, m_strbuildvalues);
                m_strbuildvalues.Clear();
                m_NumericValList.RemoveRange(0, m_NumericValList.Count);
            }
        }

        public void SaveNumericValueListConsolidatedCSV()
        {
            //This method saves all numeric data with the same relative time attribute to a list in memory till
            //the next data with a different realtive time attribute arrives, only then the first set gets exported

            if (m_NumericValList.Count != 0)
            {
                WriteNumericHeadersListConsolidatedCSV();
                string pathcsv = Path.Combine(Directory.GetCurrentDirectory(), "MPDataExport.csv");

                int firstelementreltimestamp = Convert.ToInt32(m_NumericValList.ElementAt(0).Relativetimestamp);
                int listcount = m_NumericValList.Count;

                for (int i = m_elementcount; i < listcount; i++)
                {
                    int elementreltime = Convert.ToInt32(m_NumericValList.ElementAt(i).Relativetimestamp);
                    if (elementreltime == firstelementreltimestamp)
                    {
                        m_strbuildvalues.Append(m_NumericValList.ElementAt(i).Value);
                        m_strbuildvalues.Append(',');
                        m_elementcount++;
                    }
                    else
                    {
                        m_strbuildvalues.Insert(0, ',');
                        m_strbuildvalues.Insert(0, m_NumericValList.ElementAt(0).Relativetimestamp);
                        m_strbuildvalues.Insert(0, ',');
                        m_strbuildvalues.Insert(0, m_NumericValList.ElementAt(0).Timestamp);


                        m_strbuildvalues.Remove(m_strbuildvalues.Length - 1, 1);
                        m_strbuildvalues.Replace(",,", ",");
                        m_strbuildvalues.AppendLine();

                        ExportNumValListToCSVFile(pathcsv, m_strbuildvalues);
                        m_strbuildvalues.Clear();
                        m_NumericValList.RemoveRange(0, m_elementcount);
                        m_elementcount = 0;
                        listcount = m_NumericValList.Count;
                    }
                }
                 

                
            }
        }

        public void WriteNumericHeadersListConsolidatedCSV()
        {
            if (m_NumericValList.Count != 0 && m_transmissionstart)
            {
                string pathcsv = Path.Combine(Directory.GetCurrentDirectory(), "MPDataExport.csv");

                int firstelementreltimestamp = Convert.ToInt32(m_NumericValList.ElementAt(0).Relativetimestamp);
                int listcount = m_NumValHeaders.Count;

                for(int i= m_headerelementcount;i<listcount;i++)
                {
                    int elementreltime = Convert.ToInt32(m_NumericValList.ElementAt(i).Relativetimestamp);
                    if (elementreltime == firstelementreltimestamp)
                    {
                        m_strbuildheaders.Append(m_NumValHeaders.ElementAt(i));
                        m_strbuildheaders.Append(',');
                        m_headerelementcount++;
                    }
                    else
                    {
                        m_strbuildheaders.Insert(0, ',');
                        m_strbuildheaders.Insert(0, "RelativeTime");
                        m_strbuildheaders.Insert(0, ',');
                        m_strbuildheaders.Insert(0, "Time");


                        m_strbuildheaders.Remove(m_strbuildheaders.Length - 1, 1);
                        m_strbuildheaders.Replace(",,", ",");
                        m_strbuildheaders.AppendLine();
                        ExportNumValListToCSVFile(pathcsv, m_strbuildheaders);

                        m_strbuildheaders.Clear();
                        m_NumValHeaders.RemoveRange(0, m_headerelementcount);
                        m_headerelementcount = 0;
                        listcount = m_NumValHeaders.Count;
                        m_transmissionstart = false;
                    }

                }

                
            }
        }

		public void ExportWaveToCSV()
		{
			int wavevallistcount = m_WaveValResultList.Count;

			if (wavevallistcount != 0)
			{
				
				foreach (WaveValResult WavValResult in m_WaveValResultList)
				{
					string WavValID = string.Format("{0}WaveExport.csv",WavValResult.PhysioID);

					string pathcsv = Path.Combine(Directory.GetCurrentDirectory(), WavValID);

					int wavvalarraylength = WavValResult.Value.GetLength(0);

                    for (int index = 0; index < wavvalarraylength; index++)
                    {
                        //Data sample size is 16 bits, but the significant bits represent actual sample value

                        //Read every 2 bytes
                        byte msb = WavValResult.Value.ElementAt(index);
                        byte lsb = WavValResult.Value.ElementAt(index + 1);

                        int msbval;
                        //mask depends on no. of significant bits
                        //int mask = 0x3FFF; //mask for 14 bits
                        int mask = CreateMask(WavValResult.significant_bits);

                        //int shift = (m_sample_size-8);
                        int msbshift = (msb << 8);

                        if (WavValResult.SaFlags < 0x4000)
                        {
                            msbval = (msbshift & mask);
                            msbval = (msbval >> 8);
                        }
                        else msbval = msb;
                        msb = Convert.ToByte(msbval);

                        byte[] data = { msb, lsb };
                        if (BitConverter.IsLittleEndian) Array.Reverse(data);

                        double Waveval = BitConverter.ToInt16(data, 0);
                        //Waveval = ScaleRangeSaValue(Waveval);

                        index = index + 1;

                        //if (CheckPhysiologicalRange(Waveval))
                        {
                            m_strbuildwavevalues.Append(WavValResult.Timestamp);
                            m_strbuildwavevalues.Append(',');
                            m_strbuildwavevalues.Append(Waveval.ToString());
                            m_strbuildwavevalues.Append(',');
                            m_strbuildwavevalues.AppendLine();
                        }
					}

					ExportNumValListToCSVFile(pathcsv, m_strbuildwavevalues);

					m_strbuildwavevalues.Clear();
				}

				m_WaveValResultList.RemoveRange(0, wavevallistcount);
			}
		}

        public static int CreateMask(int significantbits)
        {
            int mask = 0;

            for (int i = 0; i < significantbits; i++)
            {
                mask |= (1 << i);
            }
            return mask;
        }

        public bool CheckPhysiologicalRange(double Waveval)
        {
            if (!double.IsNaN(m_scalerangespec.lower_absolute_value) && !double.IsNaN(m_scalerangespec.upper_absolute_value))
            {
                if (Waveval >= m_scalerangespec.lower_scaled_value && Waveval <= m_scalerangespec.upper_scaled_value)
                    return true;
                else return false;
            }
            else return false;
        }

        public double ScaleRangeSaValue(double Waveval)
        {

            if (!double.IsNaN(m_scalerangespec.lower_absolute_value) && !double.IsNaN(m_scalerangespec.upper_absolute_value))
            {
                double prop = 0;
                double value = 0;
                double Wavevalue = Waveval;

                if (m_scalerangespec.upper_scaled_value != m_scalerangespec.lower_scaled_value)
                {
                    prop = 1.0 * (Waveval - m_scalerangespec.lower_scaled_value) / (m_scalerangespec.upper_scaled_value - m_scalerangespec.lower_scaled_value);
                }
                if(m_scalerangespec.upper_absolute_value != m_scalerangespec.lower_absolute_value)
                {
                    value = m_scalerangespec.lower_absolute_value + prop * (m_scalerangespec.upper_absolute_value - m_scalerangespec.lower_absolute_value);
                    
                }
                if (value != 0)
                {
                    Wavevalue = value;
                }

                return Wavevalue;
            }
            else return Waveval; 
        }
        
		public void ExportNumValListToCSVFile(string _FileName, StringBuilder strbuildNumVal)
        {
            try
            {
                // Open file for reading. 
                StreamWriter wrStream = new StreamWriter(_FileName, true, Encoding.UTF8);

                wrStream.Write(strbuildNumVal);
                strbuildNumVal.Clear();

                // close file stream. 
                wrStream.Close();

            }

            catch (Exception _Exception)
            {
                // Error. 
                Console.WriteLine("Exception caught in process: {0}", _Exception.ToString());
            }

        }

        public bool ByteArrayToFile(string _FileName, byte[] _ByteArray, int nWriteLength)
        {
            try
            {
                /*// Open file for reading. 
                FileStream _FileStream = new FileStream(_FileName, FileMode.Append, FileAccess.Write);

                // Writes a block of bytes to this stream using data from a byte array
                _FileStream.Write(_ByteArray, 0, nWriteLength);
                
                // close file stream. 
                _FileStream.Close();*/

                // Open file for reading. 
                StreamWriter wrStream = new StreamWriter(_FileName, true, Encoding.UTF8);

                String datastr = BitConverter.ToString(_ByteArray);

                wrStream.WriteLine(datastr);
                
                // close file stream. 
                wrStream.Close();


                return true;
            }

            catch (Exception _Exception)
            {
                // Error. 
                Console.WriteLine("Exception caught in process: {0}", _Exception.ToString());
            }
            // error occured, return false. 
            return false;
        }

        
    }




}
