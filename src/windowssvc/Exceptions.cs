namespace ArcusWinSvc;

/// <summary>
/// None specified error other than whats in the message
/// </summary>
/// <param name="msg">hopefully something descriptive</param>
public class DataStoreException(string msg) : Exception(msg);
