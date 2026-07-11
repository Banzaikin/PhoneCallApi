# PhoneCallApi
API для звонков с модема


## Что используется
- **Net8.0**
- **Модем Мегафон (симка Теле2)**


## Описание:
- http://192.168.8.217:10000 или https://192.168.8.217:10001
- Используется ключ (Api-key)
- make-call: В тело запроса ввести номер телефона куда будете звонить
- send-sms: В тело запроса ввести номер телефона и сообщение, которое будете отправлять по смс

**После вызова модем падает в режим "занято", модем перестает слушать команды в течение 30-40 секунд.**


## appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
    "ModemSettings": {
    "PortName": "/dev/ttyUSB0", //порт, куда подключен модем
    "BaudRate": 9600,
    "Timeout": 2000
  },
    "ApiGatewaySettings": {
    "MasterApiKey": "master_key_7x!A%D*G-KaPdSgVkYp3s6v9y$B?E(H",
    "ClientApiKeys": [
      "client_key_1LqMz4u7x!A%D*G-KaPdSgUkXp2s5v8y", //ключ
      "client_key_2B?E(H+MbQeThWmZq3t6w9z$C&F)J@N" //ключ
    ],
    "MaxRequestsPerMinute": 30,
    "AllowedIPs": [
      "192.168.1.100",
      "10.0.0.50"
    ],
    "EnableIPWhitelist": false
  }
}
```
