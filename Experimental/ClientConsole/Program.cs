using EventSystem;

namespace ClientConsole
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            // �T�[�o�ƃN���C�A���g�̃v���W�F�N�g�𕪂���K�v�͂Ȃ��B
            // �����������A�Z���u������Q�̃v���Z�X���N����������̓f�o�b�O���Â炢�̂ŕ������B
            IServerHost host = new ServerHost();
            host.Run(args);
        }
    }
}