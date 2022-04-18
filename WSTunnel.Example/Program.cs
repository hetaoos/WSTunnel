// ��������ռ�
using WSTunnel;

var builder = WebApplication.CreateBuilder(args);

//����������
builder.Services.AddWSTunnel();
// Add services to the container.

var app = builder.Build();

//���� WebSocket ֧��
app.UseWebSockets();

// �޸Ĳ����������룬������֤�������޸��¡�
// TunnelParam.param_key = System.Text.Encoding.UTF8.GetBytes("azleKxOgDmp4wV7l");

//�� WebSocket ·����ĿǰΪ /ws�������޸�Ϊ������
app.MapGet("/ws", WSTunnelRequestDelegate.Request);

app.Run();