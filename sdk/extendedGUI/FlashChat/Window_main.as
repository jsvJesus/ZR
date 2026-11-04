package
{
   import com.ChannelColors;
   import com.ChatSettings;
   import com.Commands;
   import com.GameCommunication;
   import com.ParseChannels;
   import flash.display.InteractiveObject;
   import flash.display.MovieClip;
   import flash.events.Event;
   import flash.events.KeyboardEvent;
   import flash.events.MouseEvent;
   import flash.external.ExternalInterface;
   import flash.sampler.*;
   import flash.ui.Keyboard;
   import flash.utils.getTimer;
   import flash.utils.setTimeout;
   import scaleform.clik.core.UIComponent;
   import scaleform.clik.utils.ConstrainMode;
   import scaleform.clik.utils.Constraints;
   import scaleform.clik.utils.Padding;
   
   public class Window_main extends UIComponent
   {
      public var Log:NotificationBar;
      
      public var hiddenBtn:hiddenChat;
      
      public var s1:MovieClip;
      
      public var s2:MovieClip;
      
      public var s3:MovieClip;
      
      public var s4:MovieClip;
      
      internal var Init:Boolean = false;
      
      internal var API:GameCommunication = new GameCommunication();
      
      internal var Parse:ParseChannels = new ParseChannels();
      
      public var ChatFocus:InteractiveObject;
      
      internal var Focus_lock:Boolean;
      
      public var GameMode:String = "game";
      
      public var ScreenWidth:Number = 0;
      
      public var ScreenHeight:Number = 864;
      
      public var ShowChan:Boolean = true;
      
      public var ShowTime:Boolean = true;
      
      public var radio_modes:Object = new Object();
      
      public var radio_mode:Boolean = false;
      
      public var locale:Object = new Object();
      
      public var defaultChannals:Array = new Array();
      
      public var language:String = "";
      
      public var mainSetting:* = new Array();
      
      public var fontSize:Number = 14;
      
      protected var newLocale:Boolean = false;
      
      protected var newChannels:Boolean = false;
      
      protected var cursor:Boolean = false;
      
      public var Delay:Number = 3000;
      
      protected var lastIMEString:String = "";
      
      protected var lastIMECandidates:Array;
      
      internal const N:Number = 3;
      
      public var arrayOfChanals:Array = new Array();
      
      internal var ignoreNames:Array = ["sb","s1","s2","s3","s4","Btn","Log","hiddenBtn","imeComponent"];
      
      public var MainChat:MainWind;
      
      public var Bing:BingWindow = new BingWindow();
      
      public var imeComponent:IMEComponent;
      
      public var colors:Array = new Array();
      
      public function Window_main()
      {
         ExternalInterface.addCallback("localized_resource",this.setLocaleFunc);
         ExternalInterface.addCallback("default_settings",this.loadSetting);
         ExternalInterface.addCallback("read_user_settings",this.loadSetting);
         ExternalInterface.addCallback("channel_data",this.setChannels);
         ExternalInterface.addCallback("print_msg",this.getMesssage);
         ExternalInterface.addCallback("change_locale",this.newLocalized);
         ExternalInterface.addCallback("change_mode",this.setMode);
         ExternalInterface.addCallback("returned_msg",this.returnMsg);
         ExternalInterface.addCallback("access_level",this.setAccountStatus);
         ExternalInterface.addCallback("get_locale",this.setLanguage);
         ExternalInterface.addCallback("put_to_chat",this.setWhispMessage);
         ExternalInterface.addCallback("hide_chat",this.hideChat);
         ExternalInterface.addCallback("translate",this.setTranslatedText);
         ExternalInterface.addCallback("save_settings",this.saveSettings);
         ExternalInterface.addCallback("notif_delay",this.setNotifDelay);
         ExternalInterface.addCallback("get_commands",this.setCommands);
         ExternalInterface.addCallback("show_notification",this.showHightMessage);
         ExternalInterface.addCallback("get_colors",this.setColors);
         ExternalInterface.addCallback("get_user_channel_colors",this.setUserChannelColors);
         ExternalInterface.addCallback("rotX",this.rotX);
         ExternalInterface.addCallback("rotY",this.rotY);
         ExternalInterface.addCallback("rotZ",this.rotZ);
         ExternalInterface.addCallback("clear",this.clearChat);
         ExternalInterface.addCallback("sound_settings",this.set_sound_settings);
         ExternalInterface.addCallback("ime_event",this.on_ime_event);
         super();
         stage.addEventListener(MouseEvent.MOUSE_DOWN,this.sayMouseDown);
         stage.addEventListener(Event.RESIZE,this.onResize);
         this.name = "imeComponent";
         this.imeComponent.y = 200;
         this.imeComponent.x = 200;
      }
      
      protected function setCommands(Obj:*) : *
      {
         Commands.parseCommands(Obj);
      }
      
      protected function rotX(N:Number) : *
      {
         this.MainChat.rotationX = N;
      }
      
      protected function rotY(N:Number) : *
      {
         this.MainChat.rotationY = N;
      }
      
      protected function rotZ(N:Number) : *
      {
         this.MainChat.rotationZ = N;
      }
      
      protected function setNotifDelay(N:Number) : *
      {
         this.Delay = N;
      }
      
      override protected function configUI() : void
      {
         super.configUI();
         constraints = new Constraints(this,ConstrainMode.REFLOW);
         this.MainChat.contentPadding = new Padding(50,10,20,10);
         this.Bing.name = "s1";
         this.ScreenWidth = (Object(root).width - stage.stageWidth) / 2;
         this.ScreenHeight = (Object(root).height + stage.stageHeight) / 2;
         this.hiddenBtn.visible = false;
         this.API.initialization();
      }
      
      public function saveSettings(Obj:*) : *
      {
         this.API.saveSettings(Object(root).MainChat.SaveWindows());
         this.API.updateSoundSettings(ChatSettings.getSoundschannels());
      }
      
      public function setTranslatedText(S:*) : *
      {
         this.Bing.setTranslationText(S);
      }
      
      public function MouseHideChat(Obj:*) : *
      {
         if(this.MainChat.HiddenMode)
         {
            if(this.GameMode != "game")
            {
               this.setMode("game");
            }
         }
         else if(this.GameMode != "half_hidden")
         {
            this.setMode("half_hidden");
         }
      }
      
      public function hideChat(Obj:*) : *
      {
         if(this.MainChat.HiddenMode)
         {
            if(this.GameMode != "game")
            {
               this.setMode("game");
               this.API.changeMode("game");
            }
         }
         else if(this.GameMode != "half_hidden")
         {
            this.setMode("half_hidden");
            this.API.changeMode("half_hidden");
         }
         stage.focus = null;
      }
      
      public function ResizeChildren(X:Number, Y:Number) : *
      {
         for(var i:* = 0; i < Object(root).numChildren; i++)
         {
            if(Object(root).getChildAt(i).name != "imeComponent" && Object(root).getChildAt(i).name != "Message" && Object(root).getChildAt(i).name != "s1" && Object(root).getChildAt(i).name != "s2" && Object(root).getChildAt(i).name != "s3" && Object(root).getChildAt(i).name != "s4")
            {
               Object(root).getChildAt(i).x = Object(root).getChildAt(i).x + X;
               Object(root).getChildAt(i).y = Object(root).getChildAt(i).y + Y;
               if(Object(root).getChildAt(i).name != "sb" && Object(root).getChildAt(i).name != "Log" && Object(root).getChildAt(i).name != "Btn" && Object(root).getChildAt(i).name != "hiddenBtn")
               {
                  Object(root).getChildAt(i).X = Object(root).getChildAt(i).X + X;
                  Object(root).getChildAt(i).Y = Object(root).getChildAt(i).Y + Y;
               }
            }
         }
      }
      
      public function validateChildrenPos() : *
      {
         for(var i:* = 0; i < Object(root).numChildren; i++)
         {
            if(Object(root).getChildAt(i).name != "imeComponent" && Object(root).getChildAt(i).name != "Message" && Object(root).getChildAt(i).name != "sb" && Object(root).getChildAt(i).name != "s1" && Object(root).getChildAt(i).name != "s2" && Object(root).getChildAt(i).name != "s3" && Object(root).getChildAt(i).name != "s4" && Object(root).getChildAt(i).name != "Log" && Object(root).getChildAt(i).name != "Btn" && Object(root).getChildAt(i).name != "hiddenBtn")
            {
               Object(root).getChildAt(i).validatePosition();
            }
         }
      }
      
      public function validateColors() : *
      {
         for(var i:* = 0; i < Object(root).numChildren; i++)
         {
            if(Object(root).getChildAt(i).name != "imeComponent" && Object(root).getChildAt(i).name != "Message" && Object(root).getChildAt(i).name != "sb" && Object(root).getChildAt(i).name != "s1" && Object(root).getChildAt(i).name != "s2" && Object(root).getChildAt(i).name != "s3" && Object(root).getChildAt(i).name != "s4" && Object(root).getChildAt(i).name != "Log" && Object(root).getChildAt(i).name != "Btn" && Object(root).getChildAt(i).name != "hiddenBtn")
            {
               Object(root).getChildAt(i).validateColors();
            }
         }
      }
      
      public function onResize(e:Event) : *
      {
         var X:* = undefined;
         var Y:Number = NaN;
         X = (Object(root).width - stage.stageWidth) / 2 - this.ScreenWidth;
         Y = (Object(root).height + stage.stageHeight) / 2 - this.ScreenHeight;
         this.ScreenWidth = (Object(root).width - stage.stageWidth) / 2;
         this.ScreenHeight = (Object(root).height + stage.stageHeight) / 2;
         this.ResizeChildren(X,Y);
         this.hiddenBtn.x = (Object(root).width - stage.stageWidth) / 2 + 4.5;
         this.hiddenBtn.y = (Object(root).height + stage.stageHeight) / 2 - this.hiddenBtn.height;
         if(this.GameMode != "hidden")
         {
            this.validateChildrenPos();
         }
      }
      
      public function onEnterDown(e:KeyboardEvent) : *
      {
         stage.removeEventListener(KeyboardEvent.KEY_DOWN,this.onEnterDown);
         if(e.keyCode == Keyboard.ENTER || e.keyCode == Keyboard.NUMPAD_ENTER)
         {
            if(stage.focus != null)
            {
               if(stage.focus.name != null)
               {
                  if(stage.focus.name != "inpt")
                  {
                     stage.focus = this.MainChat.inpt;
                  }
               }
            }
            else
            {
               stage.focus = this.MainChat.inpt;
            }
         }
      }
      
      public function sayChangeMode(e:MouseEvent) : *
      {
         this.API.changeMode(this.GameMode);
         stage.removeEventListener(MouseEvent.MOUSE_UP,this.sayChangeMode);
      }
      
      public function sayMouseDown(e:MouseEvent) : *
      {
         var mode:String = this.GameMode;
         if(e.eventPhase == 2)
         {
            if(this.MainChat.MissClickMode)
            {
               if(this.GameMode == "chat")
               {
                  this.MouseHideChat(null);
                  stage.addEventListener(MouseEvent.MOUSE_UP,this.sayChangeMode);
               }
            }
            else if(this.GameMode == "chat")
            {
               this.ChatFocus = stage.focus;
            }
         }
         else if(this.GameMode != "chat")
         {
            if(this.GameMode != "half_hidden" && e.target.name != "Log")
            {
               mode = "chat";
               this.GameMode = mode;
               this.setMode(this.GameMode);
               stage.addEventListener(MouseEvent.MOUSE_UP,this.sayChangeMode);
            }
         }
         else if(this.cursor)
         {
            this.cursor = false;
            this.API.changeMode("chat");
         }
      }
      
      public function DrawWindows(Ob:Object) : *
      {
         var j:* = undefined;
         var i:* = undefined;
         var tmpObj:Object = null;
         var newWind:Wind = null;
         var tmp:Object = null;
         if(Ob["colors"] != null)
         {
            this.setUserChannelColors(Ob.colors);
         }
         this.MainChat.title = this.locale.TITLE_MAIN_WINDOW;
         this.MainChat.defaultChannals = this.defaultChannals;
         this.MainChat.y = Ob["MainWindow"].y * ((Object(root).height + stage.stageHeight) / 2);
         this.MainChat.x = (Object(root).width - stage.stageWidth) / 2 + Ob["MainWindow"].x * stage.stageWidth;
         this.MainChat.onlyEng = Ob["MainWindow"].onlyEng;
         this.MainChat.height = Ob["MainWindow"].height - 12;
         this.MainChat.width = Ob["MainWindow"].width - 12;
         this.MainChat.settings = this.Parse.parse(Ob["MainWindow"].channals,this.defaultChannals,Ob["MainWindow"].sounds || new Array());
         this.MainChat.locale = this.locale;
         this.MainChat.mainMsgs = "";
         this.MainChat.HiddenMode = Ob["MainWindow"].HiddenMode;
         this.MainChat.MissClickMode = Ob["MainWindow"].MissClickMode;
         this.MainChat.changeSetDropDown(-1);
         this.MainChat.alphaGame = Ob["MainWindow"].transpGame;
         this.MainChat.alphaChat = Ob["MainWindow"].transpChat;
         this.MainChat.ShowTime = Ob["MainWindow"].time;
         this.MainChat.Scrolling = Ob["MainWindow"].scroll;
         this.MainChat.ShowChan = Ob["MainWindow"].chanal;
         this.MainChat.newWhispTabsForFriend = Ob["MainWindow"].forfriend;
         this.MainChat.newWhispTabsForAll = Ob["MainWindow"].forall;
         this.MainChat.newWhispTabs = Ob["MainWindow"].whispnewtab;
         if(Ob["MainWindow"].fontSize != null)
         {
            this.MainChat.fontSize = Ob["MainWindow"].fontSize;
            this.MainChat.fontSizeChange(Ob["MainWindow"].fontSize);
         }
         this.MainChat.whispID = this.MainChat.findWhispID();
         this.MainChat.newsID = this.MainChat.findNewsID();
         var dat:Array = new Array();
         for(j in Ob["MainWindow"].tabs)
         {
            tmpObj = new Object();
            if(!(j == this.locale.ENGLISH && this.language != "en"))
            {
               if(Ob["MainWindow"].tabs[j].defaultTab != "")
               {
                  tmpObj.label = this.locale[Ob["MainWindow"].tabs[j].defaultTab];
               }
               else
               {
                  tmpObj.label = Ob["MainWindow"].tabs[j].label;
               }
               if(j == this.locale.ENGLISH && this.language == "en")
               {
                  tmpObj.onlyEng = true;
               }
               else
               {
                  tmpObj.onluEng = Ob["MainWindow"].tabs[j].onlyEng;
               }
               tmpObj.text = "";
               tmpObj.chanalParam = this.Parse.parse(Ob["MainWindow"].tabs[j].channals,this.defaultChannals,Ob["MainWindow"].tabs[j].sounds || new Array());
               tmpObj.user = Ob["MainWindow"].tabs[j].user;
               tmpObj.whisp = Ob["MainWindow"].tabs[j].whisp;
               tmpObj.defaultTab = Ob["MainWindow"].tabs[j].defaultTab;
               dat.push(tmpObj);
            }
         }
         setTimeout(this.MainChat.TabBar.dataProvider,50,dat);
         setTimeout(this.MainChat.initRamka,10);
         for(i in Ob)
         {
            if(String(i) != "default" && String(i) != "colors")
            {
               if(String(i) != "MainWindow")
               {
                  newWind = new Wind();
                  newWind.defaultChannals = this.defaultChannals;
                  newWind.x = (Object(root).width - stage.stageWidth) / 2 + Ob[i].x * stage.stageWidth;
                  newWind.y = Ob[i].y * ((Object(root).height + stage.stageHeight) / 2);
                  newWind.height = Ob[i].height - 12;
                  newWind.width = Ob[i].width - 12;
                  newWind.settings = this.Parse.parse(Ob[i].channals,this.defaultChannals,Ob[i].sounds || new Array());
                  newWind.alphaGame = Ob[i].transpGame;
                  newWind.alphaChat = Ob[i].transpChat;
                  newWind.ShowTime = Ob[i].time;
                  newWind.Scrolling = Ob[i].scroll;
                  newWind.ShowChan = Ob[i].chanal;
                  newWind.user = Ob[i].user;
                  newWind.whisp = Ob[i].whisp;
                  newWind.onlyEnh = Ob[i].onlyEng;
                  newWind.defaultTab = Ob[i].defaultTab;
                  if(Ob[i].defaultTab != "")
                  {
                     newWind.title = this.locale[Ob[i].defaultTab];
                  }
                  else
                  {
                     newWind.title = Ob[i].label;
                  }
                  if(newWind.whisp)
                  {
                     tmp = new Object();
                     tmp.label = "@" + newWind.user;
                     tmp.selected = true;
                     tmp.id = -99;
                     tmp.com = "@" + newWind.user;
                     newWind.settings.unshift(tmp);
                  }
                  newWind.locale = this.locale;
                  addChild(newWind);
                  setTimeout(newWind.initRamka,100);
                  if(Ob[i].fontSize != null)
                  {
                     newWind.fontSized = Ob[i].fontSize;
                  }
               }
            }
         }
         this.ChatFocus = this.MainChat.inpt;
         this.validateChildrenPos();
         stage.focus = this.MainChat.inpt;
         this.API.ready();
         this.setMode("game");
      }
      
      public function setLanguage(s:String) : *
      {
         this.language = s;
         this.onLanguageChange();
      }
      
      protected function onLanguageChange() : *
      {
      }
      
      protected function showHightMessage(data:Object) : *
      {
         this.Log.Show(data);
      }
      
      override protected function draw() : void
      {
         super.draw();
      }
      
      public function setMode(mod:String) : *
      {
         if(this.getChildByName("Bing") != null)
         {
            removeChild(this.Bing);
         }
         mod = mod.toLowerCase();
         switch(mod)
         {
            case "game":
               this.ChatFocus = stage.focus;
               stage.focus = this.hiddenBtn;
               this.GameMode = mod;
               this.hiddenBtn.visible = false;
               this.hiddenBtn.State = "up";
               break;
            case "chat":
               stage.focus = this.ChatFocus;
               if(stage.focus != null)
               {
                  if(stage.focus.name != null)
                  {
                     if(stage.focus.name != "inpt")
                     {
                        stage.focus = this.MainChat.inpt;
                     }
                  }
               }
               else
               {
                  stage.focus = this.MainChat.inpt;
               }
               this.GameMode = mod;
               this.hiddenBtn.visible = false;
               this.API.changeMode(mod);
               break;
            case "hidden":
               this.ChatFocus = stage.focus;
               stage.focus = this.hiddenBtn;
               this.GameMode = mod;
               this.hiddenBtn.visible = true;
               this.hiddenBtn.y = (Object(root).height + stage.stageHeight) / 2 - this.hiddenBtn.height;
               this.hiddenBtn.x = (Object(root).width - stage.stageWidth) / 2 + 4.5;
               this.API.changeMode(mod);
               break;
            case "half_hidden":
               this.ChatFocus = stage.focus;
               this.GameMode = mod;
               this.hiddenBtn.visible = false;
               this.hiddenBtn.State = "up";
               this.API.changeMode(mod);
         }
         for(var i:* = 0; i < Object(root).numChildren; i++)
         {
            if(this.ignoreNames.indexOf(Object(root).getChildAt(i).name) == -1)
            {
               Object(root).getChildAt(i).setMode(mod);
            }
         }
         for(var j:* = 0; j < Object(root).numChildren; j++)
         {
            if(this.ignoreNames.indexOf(Object(root).getChildAt(j).name) == -1)
            {
               Object(root).getChildAt(j).setMode(mod);
            }
         }
      }
      
      public function getMesssage(Obj:*) : *
      {
         var tmp:Number = getTimer();
         var fr:Boolean = false;
         if(Obj.is_friend != null)
         {
            fr = Boolean(Obj.is_friend);
         }
         this.GetMsg(Obj.channel_id,Obj.time,Obj.text,Obj.sender,Obj.additional,fr,false,Obj.only_latin);
      }
      
      public function GetMsg(id:Number, time:String, msg:String, user:String = "", clan:String = "", friend:Boolean = false, returned:Boolean = false, onlyEng:Boolean = false) : *
      {
         var str:* = null;
         var TMP:Object = null;
         if(this.hiddenBtn.visible)
         {
            if(Object(root).MainChat.whispID == id)
            {
               this.hiddenBtn.State = "notification";
            }
         }
         if(Object(root).MainChat.newsID == id)
         {
            str = "<font color=\"#" + ChannelColors.getAt(this.MainChat.newsID).toString(16) + "\">" + msg + "</font>";
            if(this.Log != getChildAt(numChildren - 2))
            {
               swapChildren(this.Log,getChildAt(numChildren - 2));
            }
            this.Log.Show({
               "text":str,
               "show_time":0
            });
         }
         var Flag:Boolean = false;
         for(var i:* = 0; i < Object(root).numChildren; i++)
         {
            if(this.ignoreNames.indexOf(Object(root).getChildAt(i).name) == -1)
            {
               if(Object(root).getChildAt(i).drawMsg(id,time,msg,user,clan,returned,onlyEng))
               {
                  Flag = true;
               }
            }
         }
         if(!returned && Object(root).MainChat.findWhispID() == id)
         {
            if(!Flag && Boolean(Object(root).MainChat.newWhispTabs))
            {
               TMP = new Object();
               TMP.usr = user;
               TMP.msg = msg;
               TMP.id = id;
               TMP.tme = time;
               if(Object(root).MainChat.newWhispTabsForAll)
               {
                  Object(root).MainChat.TabBar.addDefTab(user,TMP);
               }
               else if(Boolean(Object(root).MainChat.newWhispTabsForFriend) && friend)
               {
                  Object(root).MainChat.TabBar.addDefTab(user,TMP);
               }
            }
         }
      }
      
      public function clearChat() : *
      {
         for(var i:* = 0; i < Object(root).numChildren; i++)
         {
            if(this.ignoreNames.indexOf(Object(root).getChildAt(i).name) == -1)
            {
               Object(root).getChildAt(i).clearChat();
            }
         }
      }
      
      public function DropAll() : *
      {
         ChannelColors.resetColors();
         for(var i:* = 0; i < Object(root).numChildren; i++)
         {
            if(this.ignoreNames.indexOf(Object(root).getChildAt(i).name) == -1)
            {
               Object(root).getChildAt(i).DropSetting();
            }
         }
         for(var j:* = 0; j < Object(root).numChildren; j++)
         {
            if(this.ignoreNames.indexOf(Object(root).getChildAt(j).name) == -1)
            {
               Object(root).getChildAt(j).DropSetting();
            }
         }
      }
      
      public function loadSetting(Obj:*) : *
      {
         if(!this.Init)
         {
            this.Init = true;
         }
         else
         {
            this.DropAll();
         }
         this.DrawWindows(Obj);
      }
      
      public function set_sound_settings(Obj:*) : *
      {
         ChatSettings.setSoundschannels(Obj);
      }
      
      public function on_ime_event(obj:*) : *
      {
         var j:* = undefined;
         var composition:String = null;
         var candidates:Array = new Array();
         var selectedIndex:int = int(obj["selectedIndex"]);
         for each(j in obj["candidates"])
         {
            candidates.push(j);
         }
         composition = obj["composition"];
         if(this.lastIMECandidates && this.lastIMECandidates.length > 0 && candidates.length == 0)
         {
            Object(root).ChatFocus.replaceText(Object(root).ChatFocus.caretIndex - this.lastIMEString.length,Object(root).ChatFocus.caretIndex,this.lastIMECandidates[selectedIndex]);
            Object(root).ChatFocus.setSelection(Object(root).ChatFocus.length,Object(root).ChatFocus.length);
            this.imeComponent.visible = false;
            this.lastIMECandidates = new Array();
            this.lastIMEString = "";
            return;
         }
         Object(root).ChatFocus.replaceText(Object(root).ChatFocus.caretIndex - this.lastIMEString.length,Object(root).ChatFocus.caretIndex,composition);
         Object(root).ChatFocus.setSelection(Object(root).ChatFocus.length,Object(root).ChatFocus.length);
         var rect:* = Object(root).ChatFocus.getCharBoundaries(Object(root).ChatFocus.caretIndex);
         this.imeComponent.showCandidates(candidates);
         this.imeComponent.y = Object(root).ChatFocus.y + Object(root).ChatFocus.parent.y - 10;
         this.imeComponent.x = !!(Object(root).ChatFocus.x + Object(root).ChatFocus.parent.x + rect) ? Number(rect.x) : 0;
         this.imeComponent.parent.setChildIndex(this.imeComponent,this.imeComponent.parent.numChildren - 1);
         this.lastIMECandidates = candidates;
         this.lastIMEString = composition;
      }
      
      public function setChannels(Obj:*) : *
      {
         var i:* = undefined;
         var tmp:Object = null;
         this.defaultChannals = new Array();
         var newColors:Array = new Array();
         for(i in Obj)
         {
            tmp = new Object();
            tmp.id = Obj[i].id;
            tmp.selected = false;
            tmp.label = this.locale[Obj[i].name.substr(5)];
            if(tmp.label == this.locale.WHISPER)
            {
               ChannelColors.whispID = Obj[i].id;
            }
            newColors.push({
               "id":Obj[i].id,
               "color":Obj[i].color
            });
            if(Obj[i].shortcut == "null")
            {
               tmp.com = null;
            }
            else
            {
               tmp.com = Obj[i].shortcut;
            }
            if(Obj[i].short_name != null)
            {
               tmp.short_name = this.locale[Obj[i].short_name.substr(5)];
            }
            this.defaultChannals.push(tmp);
            this.mainSetting.push(tmp.id);
         }
         ChannelColors.setColors(newColors);
         if(!this.Init)
         {
            this.API.loadSettings();
            this.API.sound_settings();
         }
         if(this.newLocale)
         {
            this.changeLocale();
         }
         this.newChannels = true;
         this.MainChat.defaultChannals = this.defaultChannals;
      }
      
      public function setLocaleFunc(S:*) : *
      {
         this.locale = S.Chat;
         this.hiddenBtn.label = this.locale.CHAT;
         this.newLocale = true;
         if(!this.Init)
         {
            this.API.getChannels();
         }
         if(this.newChannels)
         {
            this.changeLocale();
         }
      }
      
      public function newLocalized() : *
      {
         this.newLocale = false;
         this.newChannels = false;
         this.API.getLocal(["Chat"]);
         this.API.getChannels();
      }
      
      public function setWhispMessage(s:String) : *
      {
         this.setMode("chat");
         Object(root).MainChat.addWhispMsg(s);
      }
      
      public function returnMsg(Obj:*) : *
      {
         this.GetMsg(Obj.channel_id,Obj.time,Obj.text,Obj.receiver,"",false,true);
      }
      
      public function setAccountStatus(Obj:*) : *
      {
         Object(root).MainChat.accountStatus = Obj;
      }
      
      public function changeLocale() : *
      {
         var i:* = undefined;
         var settings:Object = null;
         var j:* = undefined;
         var settings1:Object = null;
         if(this.newLocale && this.newChannels)
         {
            for(i = 0; i < Object(root).numChildren; i++)
            {
               if(this.ignoreNames.indexOf(Object(root).getChildAt(i).name) == -1)
               {
                  if(Object(root).getChildAt(i).name == "MainChat")
                  {
                     this.MainChat.title = this.locale.TITLE_MAIN_WINDOW;
                     settings = this.Parse.unparse(this.MainChat.settings);
                     this.MainChat.settings = this.Parse.parse(settings.channels,this.defaultChannals,settings.sounds);
                     this.MainChat.locale = this.locale;
                     this.MainChat.closeOptions();
                     for(j = 0; j < this.MainChat.TabBar.Tabs.length; j++)
                     {
                        if(this.MainChat.TabBar.Tabs[j].defaultTab != "")
                        {
                           this.MainChat.TabBar.Tabs[j].label = this.locale[this.MainChat.TabBar.Tabs[j].defaultTab];
                           this.MainChat.TabBar.changeDataArray(j,this.locale[this.MainChat.TabBar.Tabs[j].defaultTab]);
                        }
                        settings1 = this.Parse.unparse(this.MainChat.TabBar.Tabs[j].setting);
                        this.MainChat.TabBar.Tabs[j].setting = this.Parse.parse(settings1.channels,this.defaultChannals,settings1.sounds);
                     }
                  }
                  else
                  {
                     Object(root).getChildAt(i).closeOptions();
                     Object(root).getChildAt(i).locale = this.locale;
                     if(Object(root).getChildAt(i).defaultTab != "")
                     {
                        Object(root).getChildAt(i).title = this.locale[Object(root).getChildAt(i).defaultTab];
                     }
                     settings = this.Parse.unparse(this.MainChat.TabBar.Tabs[j].setting);
                     Object(root).getChildAt(i).settings = this.Parse.parse(settings.channels,this.defaultChannals,settings.sounds);
                  }
               }
            }
         }
      }
      
      public function set_radio_mode(value:Boolean, wnd_name:String) : *
      {
         var i:* = undefined;
         this.radio_modes[wnd_name] = value;
         var result:Boolean = false;
         for(i in this.radio_modes)
         {
            result ||= Boolean(this.radio_modes[i]);
         }
         if(this.radio_mode != result)
         {
            this.radio_mode = result;
            this.API.set_radio_mode(result);
         }
      }
      
      protected function setColors(Obj:*) : *
      {
         var i:* = undefined;
         var arr:Array = new Array();
         for(i in Obj.colors)
         {
            arr.push(uint(Obj.colors[i]));
         }
         this.colors = arr;
      }
      
      protected function setUserChannelColors(Obj:*) : *
      {
         var i:* = undefined;
         for(i in Obj)
         {
            ChannelColors.changeColors(Obj);
         }
      }
   }
}

