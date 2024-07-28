# NiconicoJikkyoKariDemoApp
ニコニコ実況(仮)のコメントを取得するためのデモアプリ

実装内容は下記を参照して、C#(WPF)に書き換えたもの。  
[n-air-app/n-air-app](https://github.com/n-air-app/n-air-app/blob/n-air_development/app/services/nicolive-program/NdgrClient.ts)

.proto ファイルは下記から流用  
[n-air-app/nicolive-comment-protobuf](https://github.com/n-air-app/nicolive-comment-protobuf)

## 躓いた点とか
レスポンスで返ってくるバイナリを全部投げてパースさせようとすると途中までしかパースができなかった事。  
上記の参考元プログラムを元にバッファを使って1つ1つパースさせることで全てパースさせることが出来た。

## 依存ライブラリ
|Nuget名|利用目的|
|:----|:----|
|Google.Protobuf|Protobufで自動生成されたコードを利用する|
|Grpc.Tools　|.protoファイルをVSで自動ビルドする（コマンドなどで手動でビルドする場合不要）|

## イメージ
![image](https://github.com/user-attachments/assets/b4bcc58f-8999-402b-b1f4-4ebfa5b0c6da)
