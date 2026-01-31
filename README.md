<img src="./EchoPBX.Frontend/public/echopbx.svg" alt="drawing" width="128"/>

# EchoPBX

EchoPBX is a PBX that aims to be simple to set up. It provides you with an easy-to-use web interface
for managing extensions, trunks and queues. It is built on top of [Asterisk](https://www.asterisk.org/),

The primary target audience for EchoPBX is for those who want to self-host their VoIP solution, without the
hassle that usually comes with it.

My initial vision was something along the lines of "just install this and make your life easier".

# Installation

Make sure [Docker](https://www.docker.com/) is installed on your system. If not, you can install it for
Ubuntu-based distributions using:

```shell
sudo apt install docker.io
```

Then, run the container

```shell
sudo docker run -t -p 5060:5060 -p 8740:8740 -v /opt/echopbx:/data ghcr.io/ljfloor/echopbx:main
```

After which you can access the Web UI by going to http://(serverip):8740.

# FAQ

**Can I install EchoPBX on Windows?**

EchoPBX uses [Asterisk](https://www.asterisk.org/) under the hood. Asterisk does not support Windows, hence you **cannot** install EchoPBX for Windows directly.
However, you **can** install [Docker Desktop](https://www.docker.com/products/docker-desktop/) on Windows, and run EchoPBX on that as a Docker container.

**Besides SIP, are other protocols available?**

Nope. Since SIP is by far the most used VoIP protocol, EchoPBX only supports SIP to keep things simple.
If you need other protocols like IAX2, you can install FreePBX, which is very feature rich.

Maybe in the future when there is nothing to do. Let's just say that this isn't my priority right now.

**So this is "just another frontend" for Asterisk?**

Yes and no. While EchoPBX does provide a web interface for managing Asterisk, it also aims to simplify the overall experience of setting up and managing a PBX.
It's not just a web interface that replaces config files with bare textboxes. EchoPBX tries to guide you through it, explaining concepts,
and providing sensible defaults. In the same way that Plex isn't just a fancy browser for your media files, but a whole lot of features (transcoding, metadata fetching, etc) to make your media experience better.

When you typically make a front-end for an already existing backend, you tend to make it very close to the backend. Every key in the config file translates to a textbox or checkbox in the UI.
This is not the case with EchoPBX. EchoPBX abstracts away many of the complexities of Asterisk.
This might not be for everyone though, and I respect that. So if you want a more traditional approach, FreePBX is a great alternative.

I hope this more clarifies my idea and vision, and why things like custom dialplan scripting are not available in EchoPBX.

# Building

```
docker build -t echopbx .
```

# Developing locally

The easiest way to develop locally is not with docker, but to install the dependencies directly on your machine.
Because EchoPBX uses Asterisk, you will need to install and configure Asterisk first.

Like said before, Asterisk only runs on Linux, so you will need a Linux machine or a Linux VM. Alternatively,
you can use [Windows Subsystem for Linux (WSL2)](https://learn.microsoft.com/en-us/windows/wsl/install) on Windows 10/11.

First, install asterisk and other dependencies:

```shell
sudo apt install asterisk ffmpeg
```

Disable it auto-starting, since EchoPBX will start and stop asterisk, not the system.

```shell
sudo systemctl disable asterisk --now
```

Because we run asterisk as our normal user, we need to set some permissions

```shell
sudo chown -R $USER /etc/asterisk
sudo chown -R $USER /var/spool/asterisk
sudo chown -R $USER /var/lib/asterisk
```

Copy the rootfs files to your system. You shouldn't get any permission denied errors here, since you just changed the ownership of the asterisk directories.

```shell
cp -rv rootfs/* /
```

Lastly, give yourself permission to the run dirctory, so EchoPBX can run `asterisk -r` commands:

```shell
sudo mkdir /var/run/asterisk && sudo chown -R $USER /var/run/asterisk
```

> Note: Every time you reboot your pc, you will need to run the above command again, since `/var/run` is cleared on reboot.



