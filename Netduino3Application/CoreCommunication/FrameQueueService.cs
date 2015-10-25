using System;
using Microsoft.SPOT;

namespace CoreCommunication
{
    public delegate void SendFrameEventHandler(Frame frame);
    public delegate void Callback(Frame response);

    class FrameQueueService
    {
        public static int NOTFOUND = -1;
        public event SendFrameEventHandler SendFrame;

        private struct FrameWithHandler
        {
            public Frame frame;
            public Callback callback;
        }

        private Queue<FrameWithHandler> waitingForResponseQueue;
        private Queue<FrameWithHandler> waitingForSendingQueue;

        public FrameQueueService()
        {
            waitingForResponseQueue = new Queue<FrameWithHandler>();
            waitingForSendingQueue = new Queue<FrameWithHandler>();
        }

        public void EnqueueFrame(Frame frame, Callback callback)
        {
            int frameID = UnusedFrameId;
            if (frameID == NOTFOUND)
            {
                waitingForSendingQueue.Enqueue(new FrameWithHandler { frame = frame , callback = callback });
                return;
            }

            if (callback != null)
            {
                frame.FrameID = (byte)frameID;
                waitingForResponseQueue.Enqueue(new FrameWithHandler { frame = frame, callback = callback });
            }
            else
            {
                frame.FrameID = 0;
            }
            
            SendFrame(frame);
        }

        public void onReceivedRemoteFrame(object sender, Frame frame)
        {
            ResponseReceived(frame);
        }

        public void ResponseReceived(Frame response)
        {
            int i = 0;
            foreach (FrameWithHandler element in waitingForResponseQueue)
            {
                Frame f = element.frame;
                if (f.FrameID == response.FrameID)
                {
                    if (!(response is RemoteATCommandResponseFrame) || isResponseForRequest(response, f))
                    {
                        if (element.callback != null)
                        {
                            element.callback(response);
                        }
                        break;
                    }
                }
                ++i;
            }

            waitingForResponseQueue.RemoveAt(i);
        }

        private bool isResponseForRequest(Frame response, Frame request)
        {
            return response is RemoteATCommandResponseFrame &&
                request is RemoteATCommandRequestFrame &&
                (Frame.isEqualAddress((request as RemoteATCommandRequestFrame).DestinationAddress16Bit, (response as RemoteATCommandResponseFrame).SourceAddress16Bit) ||
                (Frame.isEqualAddress((request as RemoteATCommandRequestFrame).DestinationAddress16Bit, new byte[] { 0xFF, 0xFE }) &&
                Frame.isEqualAddress((request as RemoteATCommandRequestFrame).DestinationAddress64Bit, (response as RemoteATCommandResponseFrame).SourceAddress64Bit)));
}

        public int UnusedFrameId
        {
            get
            {
                if (waitingForResponseQueue.Count == 255)
                {
                    return NOTFOUND;
                }

                for (int frameId = 1; frameId < 256; ++frameId)
                {
                    bool used = false;
                    foreach (Frame f in waitingForResponseQueue)
                    {
                        if (frameId == f.FrameID)
                        {
                            used = true;
                        }
                    }

                    if (!used) { return frameId; }
                }

                return NOTFOUND;
            }
        }
    }
}
