namespace LukeBot
{
    internal interface IUserManager
    {
        /**
         * Will be called if there is an attempt to authenticate a user.
         *
         * Example: ServerCLI receives a Login message, and during processing will call
         * this method to check if password is correct.
         *
         * Returns user's Permission level if authentication succeeded. In case of auth failure
         * should return UserPermissionLevel.None.
         */
        UserPermissionLevel AuthenticateUser(string user, byte[] pwdHash, out string reason);

        /**
         * Will be called if there is an attempt to change user's password.
         *
         * Example: ServerCLI receives a PasswordChange message, and during processing will
         * call this method to check if password can be changed.
         *
         * Should return true upon success and false upon failure. Additionally, @p reason
         * should be set when authentication fails to provide a reason why.
         */
        bool ChangeUserPassword(string user, byte[] currentPwdHash, byte[] newPwdHash, out string reason);
    }
}