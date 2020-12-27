using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using LitJson;
using System.Text;
using System.Security.Cryptography;
using System;

public class QRPanelController : MonoBehaviour {

	public QRCodeEncodeController e_qrController;
	public RawImage qrCodeImage;
	public string qrEncodeString;

	// Use this for initialization
	void Start () {
		if (e_qrController != null) {
			e_qrController.onQREncodeFinished += qrEncodeFinished;//Add Finished Event
		}
        Encode();
	}
	
	void qrEncodeFinished(Texture2D tex)
	{
		if (tex != null && tex != null) {
			qrCodeImage.texture = tex;
		} else {

		}
	}

	public void Encode()
	{
		if (e_qrController != null) {
			e_qrController.onQREncodeFinished += qrEncodeFinished;
			string valueStr = encryptData(qrEncodeString);
			e_qrController.Encode(valueStr);
			Debug.Log("Unencrypted string: "+qrEncodeString+"  Encrypted string: "+valueStr);
		}
	}

	public void ConstructAndEncodeQR () {
		string[] myEncodeArray = new string[4];
		myEncodeArray[0] = "homebase";
		myEncodeArray[1] = GameManager.instance.userId;
		myEncodeArray[2] = GameManager.instance.homebase_lat.ToString();
		myEncodeArray[3] = GameManager.instance.homebase_lon.ToString();

		string myResult = JsonMapper.ToJson(myEncodeArray);
		Debug.Log(myResult);
		qrEncodeString = myResult;
		Encode();
	}

	public string encryptData(string toEncrypt)
	{
		byte[] keyArray = UTF8Encoding.UTF8.GetBytes(GameManager.QR_encryption_key);
		// 256 -AES key 
		byte[] toEncryptArray = UTF8Encoding.UTF8.GetBytes(toEncrypt);
		RijndaelManaged rDel = new RijndaelManaged();
		rDel.Key = keyArray;
		rDel.Mode = CipherMode.ECB;
		rDel.Padding = PaddingMode.PKCS7;
		ICryptoTransform cTransform = rDel.CreateEncryptor();
		byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);

		return Convert.ToBase64String(resultArray, 0, resultArray.Length);
	}
/*
public string decryptData(string toDecrypt)
	{
		byte[] keyArray = UTF8Encoding.UTF8.GetBytes("12345678901234567890123456789012");
		// AES-256 key 
		byte[] toEncryptArray = Convert.FromBase64String(toDecrypt);
		RijndaelManaged rDel = new RijndaelManaged();
		rDel.Key = keyArray;
		rDel.Mode = CipherMode.ECB;
		rDel.Padding = PaddingMode.PKCS7; // better lang support 
		ICryptoTransform cTransform = rDel.CreateDecryptor();
		byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);

		return UTF8Encoding.UTF8.GetString(resultArray);
	}
	*/
	//this is the function to decrypt the encoded string.
}
