namespace QuranBlazor.Services
{
    /// <summary>
    /// Centralized error message constants to avoid magic strings in components
    /// </summary>
    public static class ErrorMessages
    {
        // General errors
        public const string LoadError = "Маълумотларни юклашда хатолик. Илтимос, қайта уриниб кўринг.";
        public const string NetworkError = "Интернет рабтасида хатолик";
        public const string DatabaseError = "Маалумотлар сақламасида хатолик";
        public const string GenericError = "Нотўғри бир нарса юз берди";

        // Notes
        public const string NoteEmptyMessage = "Сизда ҳали қайдлар йўқ";
        public const string NoteEmptyDescription = "Оятларга қайд қўшиш учун қайд белгисига босинг";

        // Favorites
        public const string FavoriteEmptyMessage = "Сизда ҳали белгиланган оятлар йўқ";
        public const string FavoriteEmptyDescription = "Оятларни белгилаш учун қалб белгисига босинг";

        // Collections
        public const string CollectionsEmptyMessage = "Сизда ҳали тўпламлар йўқ";
        public const string CollectionsEmptyDescription = "Оятларни тўплай бошлаш учун янги тўплам яратинг";
        public const string CollectionAyasEmptyMessage = "Бу тўпламда ҳали оятлар йўқ";

        // Validation
        public const string RequiredFieldEmpty = "Илтимос, барча майдонларни тўлдиринг";
        public const string InvalidInput = "Нотўғри киритиш. Қайта уриниб кўринг.";

        // Confirmations
        public const string DeleteConfirmationTitle = "Ишончингиз комилми?";
        public const string DeleteConfirmationMessage = "Бу амални бўйвча қайтариб бўлмайди";
    }
}
