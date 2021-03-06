﻿//-----------------------------------------------------------------------
// <copyright file="SyncSerialPort.cs" company="YuGuan Corporation">
//     Copyright (c) YuGuan Corporation. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace DevLib.Utilities
{
    using System;
    using System.IO.Ports;
    using System.Threading;

    /// <summary>
    /// Enum SerialState.
    /// </summary>
    public enum SerialStateEnum
    {
        /// <summary>
        /// Represents Init.
        /// </summary>
        Init = 0,

        /// <summary>
        /// Represents Operate OK.
        /// </summary>
        OperateOK,

        /// <summary>
        /// Represents Not Found Port.
        /// </summary>
        NotFoundPort,

        /// <summary>
        /// Represents Not Exist Port.
        /// </summary>
        NotExistPort,

        /// <summary>
        /// Represents Serial Port Null.
        /// </summary>
        SerialPortNull,

        /// <summary>
        /// Represents Open Exception.
        /// </summary>
        OpenException,

        /// <summary>
        /// Represents Send Exception.
        /// </summary>
        SendException,

        /// <summary>
        /// Represents Send Data Empty.
        /// </summary>
        SendDataEmpty,

        /// <summary>
        /// Represents Read Timeout.
        /// </summary>
        ReadTimeout,

        /// <summary>
        /// Represents Read Exception.
        /// </summary>
        ReadException
    }

    /// <summary>
    /// Represents a sync serial port resource.
    /// </summary>
    public class SyncSerialPort : IDisposable
    {
        /// <summary>
        /// Static Field _portNames.
        /// </summary>
        private static string[] _portNames = SerialPort.GetPortNames();

        /// <summary>
        /// Field _serialPort.
        /// </summary>
        private SerialPort _serialPort = null;

        /// <summary>
        /// Field _currentState.
        /// </summary>
        private SerialStateEnum _currentState = SerialStateEnum.Init;

        /// <summary>
        /// Field _receiveBuffer.
        /// </summary>
        private byte[] _receiveBuffer = null;

        /// <summary>
        /// Field _readTimeout.
        /// </summary>
        private int _readTimeout = 1000;

        /// <summary>
        /// Field _autoResetEvent.
        /// </summary>
        private AutoResetEvent _autoResetEvent = new AutoResetEvent(false);

        /// <summary>
        /// Field _syncRoot.
        /// </summary>
        private object _syncRoot = new object();

        /// <summary>
        /// Field _disposed.
        /// </summary>
        private bool _disposed = false;

        /// <summary>
        /// Initializes a new instance of the SyncSerialPort class using the specified port name, baud rate, parity bit, data bits, and stop bit.
        /// </summary>
        /// <param name="portName">The port to use (for example, COM1).</param>
        /// <param name="baudRate">The baud rate.</param>
        /// <param name="partity">One of the System.IO.Ports.SerialPort.Parity values.</param>
        /// <param name="dataBits">The data bits value.</param>
        /// <param name="stopBits">One of the System.IO.Ports.SerialPort.StopBits values.</param>
        public SyncSerialPort(string portName, int baudRate, Parity partity, int dataBits, StopBits stopBits)
        {
            if (_portNames == null || _portNames.Length == 0)
            {
                this._currentState = SerialStateEnum.NotExistPort;
            }
            else
            {
                bool isExistPort = false;

                for (int i = 0; i < _portNames.Length; i++)
                {
                    if (_portNames[i].Equals(portName))
                    {
                        isExistPort = true;
                        break;
                    }
                }

                if (isExistPort)
                {
                    this._serialPort = new SerialPort(portName, baudRate, partity, dataBits, stopBits);
                    this._currentState = SerialStateEnum.OperateOK;
                }
                else
                {
                    this._currentState = SerialStateEnum.NotFoundPort;
                }
            }
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="SyncSerialPort" /> class.
        /// </summary>
        ~SyncSerialPort()
        {
            this.Dispose(false);
        }

        /// <summary>
        /// Gets an array of serial port names for the current computer.
        /// </summary>
        public static string[] PortNames
        {
            get { return _portNames; }
        }

        /// <summary>
        /// Gets current serial state.
        /// </summary>
        public SerialStateEnum CurrentState
        {
            get { return this._currentState; }
        }

        /// <summary>
        /// Gets a value indicating whether the open or closed status of SyncSerialPort.
        /// </summary>
        public bool IsOpen
        {
            get { return this._serialPort.IsOpen; }
        }

        /// <summary>
        /// Opens a new serial port connection.
        /// </summary>
        /// <returns>true if succeeded; otherwise, false.</returns>
        public bool Open()
        {
            this.CheckDisposed();

            if (this._serialPort != null)
            {
                try
                {
                    if (!this._serialPort.IsOpen)
                    {
                        this._serialPort.Open();
                    }

                    return true;
                }
                catch
                {
                    this._currentState = SerialStateEnum.OpenException;
                    return false;
                }
            }
            else
            {
                this._currentState = SerialStateEnum.SerialPortNull;
                return false;
            }
        }

        /// <summary>
        /// Closes the port connection.
        /// </summary>
        /// <returns>true if succeeded; otherwise, false.</returns>
        public bool Close()
        {
            this.CheckDisposed();

            if (this._serialPort != null)
            {
                if (this._serialPort.IsOpen)
                {
                    this._serialPort.Close();
                }

                return true;
            }
            else
            {
                this._currentState = SerialStateEnum.SerialPortNull;
                return false;
            }
        }

        /// <summary>
        /// Sync send a specified number of bytes to the serial port using data from a buffer.
        /// </summary>
        /// <param name="sendData">The byte array that contains the data to write to the port.</param>
        /// <param name="receivedData">The byte array to write the received data.</param>
        /// <param name="timeout">The number of milliseconds before a time-out occurs when a read operation does not finish.</param>
        /// <returns>true if succeeded; otherwise, false.</returns>
        public bool Send(byte[] sendData, out byte[] receivedData, int timeout)
        {
            this.CheckDisposed();

            lock (this._syncRoot)
            {
                this._autoResetEvent.Reset();

                if (sendData == null || sendData.Length == 0)
                {
                    receivedData = new byte[0];
                    this._currentState = SerialStateEnum.SendDataEmpty;
                    return false;
                }

                if (this._serialPort == null)
                {
                    receivedData = new byte[0];
                    this._currentState = SerialStateEnum.SerialPortNull;
                    return false;
                }

                this._readTimeout = timeout;

                try
                {
                    if (!this.Open())
                    {
                        receivedData = new byte[0];
                        return false;
                    }

                    this._serialPort.Write(sendData, 0, sendData.Length);
                    Thread threadReceive = new Thread(new ParameterizedThreadStart(this.SyncReceiveData));
                    threadReceive.IsBackground = true;
                    ////threadReceive.Name = "ReadSerialPortData";
                    threadReceive.Start(this._serialPort);
                    this._autoResetEvent.WaitOne();

                    if (threadReceive.IsAlive)
                    {
                        threadReceive.Abort();
                    }

                    if (this._currentState == SerialStateEnum.OperateOK)
                    {
                        receivedData = this._receiveBuffer;
                        return true;
                    }
                    else
                    {
                        receivedData = new byte[0];
                        return false;
                    }
                }
                catch
                {
                    receivedData = new byte[0];
                    this._currentState = SerialStateEnum.SendException;
                    return false;
                }
            }
        }

        /// <summary>
        /// Releases all resources used by the current instance of the <see cref="SyncSerialPort" /> class.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases all resources used by the current instance of the <see cref="SyncSerialPort" /> class.
        /// protected virtual for non-sealed class; private for sealed class.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (this._disposed)
            {
                return;
            }

            this._disposed = true;

            if (disposing)
            {
                // dispose managed resources
                ////if (managedResource != null)
                ////{
                ////    managedResource.Dispose();
                ////    managedResource = null;
                ////}

                if (this._autoResetEvent != null)
                {
                    this._autoResetEvent.Close();
                    this._autoResetEvent = null;
                }

                if (this._serialPort != null)
                {
                    this._serialPort.Dispose();
                    this._serialPort = null;
                }
            }

            // free native resources
            ////if (nativeResource != IntPtr.Zero)
            ////{
            ////    Marshal.FreeHGlobal(nativeResource);
            ////    nativeResource = IntPtr.Zero;
            ////}
        }

        /// <summary>
        /// Sync receive data.
        /// </summary>
        /// <param name="serialPortobj">Instance of SerialPort.</param>
        private void SyncReceiveData(object serialPortobj)
        {
            SerialPort serialPort = serialPortobj as SerialPort;
            System.Threading.Thread.Sleep(0);
            serialPort.ReadTimeout = this._readTimeout;

            try
            {
                byte firstByte = Convert.ToByte(serialPort.ReadByte());
                int bytesRead = serialPort.BytesToRead;
                this._receiveBuffer = new byte[bytesRead + 1];
                this._receiveBuffer[0] = firstByte;

                for (int i = 1; i <= bytesRead; i++)
                {
                    this._receiveBuffer[i] = Convert.ToByte(serialPort.ReadByte());
                }

                this._currentState = SerialStateEnum.OperateOK;
            }
            catch
            {
                this._currentState = SerialStateEnum.ReadTimeout;
            }
            finally
            {
                this._autoResetEvent.Set();
                this.Close();
            }
        }

        /// <summary>
        /// Method CheckDisposed.
        /// </summary>
        private void CheckDisposed()
        {
            if (this._disposed)
            {
                throw new ObjectDisposedException("DevLib.Utilities.SyncSerialPort");
            }
        }
    }
}
