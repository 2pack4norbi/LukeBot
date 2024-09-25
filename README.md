# LukeBot

**TODO** This readme is just a temporary one, but I wanted a place to jot something down for the
future. I'll fill it in one day, I promise.

## Development

Build using `dotnet build`. There are three configurations available:
- `Release` - builds with optimization and logs disabled, build specifically for production.
- `Debug` - disables optimization and enables debug logs.
- `SecureDebug` - `Debug` but with secure logs added. Secure logs might contain sensitive information
  (API keys etc) so use only for local development.

LukeBot only exposes HTTPS connections, it used to support unencrypted classic HTTP while it was
run only locally, but wanting to migrate it to a web server I decided to move to HTTPS fully. To
make it work for dev work, you have to make sure some things in your system are done:

- Make sure a dev certificate has been added to your system. You can do it with `dotnet dev-certs
  https --check` command. If it's not available, call `dotnet dev-certs https --trust` and reload
  your browser window.
- Technically there should be no need to do anything else, but Firefox can potentially not support
  third-party certificates from the system's store by default. Make sure to check if Settings >
  Privacy and Security > Certificates has "Allow Firefox to automatically trust third-party root
  certificates you install" enabled (and if you had to enable it, reload the window).

There should be no warning messages related to HTTPS outputted by your browser, otherwise widget's
SSL connections can potentially not work.