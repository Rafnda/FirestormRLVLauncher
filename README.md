# FirestormRLVLauncher
Launch and automatically log in to Firestorm with RLV enabled. The password will be encrypted and hidden for a set time.  
(Currently only supports Windows.)

**Warning**: Use at your own risk. Especially because this is a new tool, potentially with bugs. Make sure it works with your environment first and ALWAYS have a second place to store your password.

<img width="602" height="432" alt="screen" src="https://github.com/user-attachments/assets/4f5a5fe2-f902-424a-82e5-f04a80179737" />


## Usage
* (Change the password of your account and associated eMail to the same long/complicated password and keep it in a text editor until done)
* (Save the password to https://app.thetimecapsule.org to set a release date)
* Select your Firestorm executable. Probably C:\Program Files\Firestorm-Releasex64\Firestorm-Releasex64.exe  
   Note: This file *has to stay unchanged*. Updates to Firestorm will make the launcher refuse to login.
* Create a new Login file with the SL user name and password. Select the duration it will stay locked. This is the time actually logged into the game.
* Test the launcher by clicking the Login button. It should launch Firestorm and log into the game. The Firestorm window has to stay in the foreground during launch or at least once it has loaded. 
* When you are sure everything is set up correctly and works after a restart, click the Lock button in the launcher
* (In Firestorm on the login window, delete the saved login for that account)
* (Delete any other copies of the new password)

## Additional Precautions
If you use longer timers, consider these precautions:
* Make a backup of the used Firestorm installer, this launcher, and the created login.enc file. As only the exact same version of Firestorm is accepted
* Additionally save the login in a full time RLV viewer like Kokua

## Disclaimer
Again, use at your own risk.

I won't and probably can't help you recover an account if something goes wrong, as that would defeat the purpose.
