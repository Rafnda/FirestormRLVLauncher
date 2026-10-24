# FirestormRLVLauncher
This launcher makes sure RLV stays on with the normal Firestorm viewer for better immersion.  
It launches Firestorm and automatically adjusts settings and logs into SL. The password is stored encrypted by the launcher (and not in Firestorm) until a set timer expires.  
(Currently only supports Windows.)

**Warning**: Use at your own risk. Especially because this is a new tool, potentially with bugs. Make sure it works with your environment first and ALWAYS have a second place to store your password.

<img width="602" height="432" alt="screen" src="https://github.com/user-attachments/assets/4f5a5fe2-f902-424a-82e5-f04a80179737" />


## Usage
* (Change the password of your account and associated eMail to the same long/complicated password and keep it in a text editor until done)
* (Save the password to https://app.thetimecapsule.org to set a release date)
* Select your Firestorm executable. Probably C:\Program Files\Firestorm-Releasex64\Firestorm-Releasex64.exe  
   Note: This file *has to stay unchanged*. Updates to Firestorm will make the launcher refuse to login.
* Create a new Login file with the SL user name and password. Select the duration it will stay locked. This is online time logged into the game.
* Test the launcher by clicking the Login button. It should launch Firestorm and log into the game on its own.
* When you are sure everything is set up correctly and it still works after a restart, click the Lock button in the launcher
* (In Firestorm on the login window, delete the saved login for that account with the trash bin button)
* (Delete any other copies of the new password)

## Additional Precautions
If you use longer timers, consider these precautions:
* Make a backup of the used Firestorm installer, this launcher, and the created login.enc file. As only the exact same version of Firestorm is accepted
* Additionally save the login in a full time RLV viewer like Kokua FTRLV

## Disclaimer
Again, use at your own risk.

I won't and probably can't help you recover an account if something goes wrong, as that would defeat the purpose.
