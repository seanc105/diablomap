namespace Database {
    public interface IDatabaseModel {
        IDatabaseModel Initialize(System.Data.IDataReader reader);
    }
}