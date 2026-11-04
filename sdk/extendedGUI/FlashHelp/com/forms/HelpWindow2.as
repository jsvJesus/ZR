package com.forms
{
   import com.components.Component;
   import com.components.SOCheckBox;
   import flash.display.Bitmap;
   import flash.display.Loader;
   import flash.display.Sprite;
   import flash.events.Event;
   import flash.events.MouseEvent;
   import flash.events.TextEvent;
   import flash.events.TimerEvent;
   import flash.external.ExternalInterface;
   import flash.net.URLRequest;
   import flash.text.Font;
   import flash.text.TextField;
   import flash.text.TextFieldAutoSize;
   import flash.text.TextFormat;
   import flash.utils.Timer;
   import flash.utils.setTimeout;
   
   public class HelpWindow2 extends Component
   {
      protected var _title:TextField;
      
      protected var _main_title:TextField;
      
      protected var _mainHelp:TextField;
      
      protected var _titleBackground:Sprite;
      
      protected var _background:Sprite;
      
      protected var _contentDict:Object = new Object();
      
      protected var _closeButton:SOButton;
      
      protected var _additionalButtons:Array = new Array();
      
      protected var _cbDontShow:SOCheckBox;
      
      protected var _nextButton:NextButton;
      
      protected var _backButton:PrevButton;
      
      protected var _playButton:PlayButton;
      
      protected var _timer:Timer;
      
      protected var _picture:Sprite;
      
      protected var _step_title:TextField;
      
      protected var _descriprion:TextField;
      
      protected var _descriprionBackground:Sprite;
      
      protected var _current_step:int = -1;
      
      protected var _current_picture:Bitmap = new Bitmap();
      
      protected var currentHelp:String = "";
      
      public var WIDTH:uint = 1024;
      
      public var HEIGHT:uint = 820;
      
      protected var PICTURE_WIDTH:uint = this.WIDTH - 28 * 2;
      
      protected var PICTURE_HEIGHT:uint = this.PICTURE_WIDTH / (16 / 9);
      
      public function HelpWindow2()
      {
         super();
         var regular:Font = new GUIRegular();
         this.drawHead();
         this.drawContent();
         this._timer = new Timer(20000);
         this._timer.addEventListener(TimerEvent.TIMER,this.nextStep);
         ExternalInterface.addCallback("set_data",this.onConfigRecieved);
         ExternalInterface.addCallback("showHelpByType",this.onShow);
         ExternalInterface.addCallback("showMainHelp",this.onShowMainHelp);
         ExternalInterface.addCallback("hide",this.onHide);
         setTimeout(ExternalInterface.call,500,"ready");
         setTimeout(ExternalInterface.call,0,"ready");
      }
      
      protected function drawHead() : *
      {
         this._title = new TextField();
         this._titleBackground = new Sprite();
         this._titleBackground.graphics.beginFill(0,0.9);
         this._titleBackground.graphics.drawRect(0,0,this.WIDTH,40);
         this._titleBackground.graphics.endFill();
         this._title.x = 13;
         this._title.y = 6;
         this._title.selectable = false;
         this._title.autoSize = TextFieldAutoSize.NONE;
         this._title.width = this.WIDTH - 13;
         this._title.defaultTextFormat = new TextFormat("Roboto Condensed Regular",24,16777215);
         this._title.text = "ИНФОРМАЦИЯ (F1):";
         this._title.addEventListener(MouseEvent.MOUSE_DOWN,this.onWindowStartDrag,false,0,true);
         this._main_title = new TextField();
         this._main_title.x = 13;
         this._main_title.y = 6;
         this._main_title.selectable = false;
         this._main_title.autoSize = TextFieldAutoSize.NONE;
         this._main_title.width = this.WIDTH - 13;
         this._main_title.defaultTextFormat = new TextFormat("Roboto Condensed Regular",24,16777215);
         this._main_title.text = "ИНФОРМАЦИЯ (F1):";
         this._main_title.addEventListener(MouseEvent.MOUSE_DOWN,this.onWindowStartDrag,false,0,true);
         this._main_title.visible = false;
         addChild(this._titleBackground);
         addChild(this._title);
         addChild(this._main_title);
      }
      
      protected function drawContent() : *
      {
         this._background = new Sprite();
         this._background.graphics.beginFill(0,0.6);
         this._background.graphics.drawRect(0,0,this.WIDTH,this.HEIGHT - 41);
         this._background.graphics.endFill();
         this._background.x = 0;
         this._background.y = 41;
         this._mainHelp = new TextField();
         this._mainHelp.autoSize = TextFieldAutoSize.NONE;
         this._mainHelp.defaultTextFormat = new TextFormat("Roboto Condensed Regular",18,16777215,null,null,null,null,null,null,null,13,13);
         this._mainHelp.x = 28;
         this._mainHelp.y = 69;
         this._mainHelp.height = this.HEIGHT - this._mainHelp.y;
         this._mainHelp.width = this.PICTURE_WIDTH;
         this._mainHelp.multiline = true;
         this._mainHelp.wordWrap = true;
         this._mainHelp.visible = false;
         this._mainHelp.addEventListener(TextEvent.LINK,this.onHelpLink);
         this._picture = new Sprite();
         this._picture.x = 28;
         this._picture.y = 69;
         this._picture.graphics.beginFill(0,0.1);
         this._picture.graphics.drawRect(0,0,this.PICTURE_WIDTH,this.PICTURE_HEIGHT);
         this._picture.graphics.endFill();
         this._picture.addEventListener(MouseEvent.CLICK,this.nextStep);
         this._step_title = new TextField();
         this._step_title.autoSize = TextFieldAutoSize.LEFT;
         this._step_title.defaultTextFormat = new TextFormat("Roboto Condensed Regular",24,16777215,null,null,null,null,null,null,null,13,13);
         this._step_title.text = "ШАГ1";
         this._step_title.background = true;
         this._step_title.backgroundColor = 0;
         this._step_title.selectable = false;
         this._step_title.height = 31;
         this._step_title.y = this.PICTURE_HEIGHT - 31;
         this._descriprion = new TextField();
         this._descriprion.multiline = true;
         this._descriprion.wordWrap = true;
         this._descriprion.autoSize = TextFieldAutoSize.NONE;
         this._descriprion.defaultTextFormat = new TextFormat("Roboto Condensed Regular",18,16777215);
         this._descriprion.selectable = false;
         this._descriprion.width = this.WIDTH - 28 * 2;
         this._descriprion.height = this.HEIGHT - this.PICTURE_HEIGHT - this._picture.y - 28 - 40;
         this._descriprion.x = 28;
         this._descriprion.y = 41 + 28 + this.PICTURE_HEIGHT + 28;
         this._descriprionBackground = new Sprite();
         this._descriprionBackground.graphics.beginFill(0,0.9);
         this._descriprionBackground.graphics.drawRect(0,0,this.WIDTH,this.HEIGHT - this.PICTURE_HEIGHT - this._picture.y - 14);
         this._descriprionBackground.graphics.endFill();
         this._descriprionBackground.x = 0;
         this._descriprionBackground.y = this._descriprion.y - 14;
         this._nextButton = new NextButton();
         this._nextButton.x = this.WIDTH / 2;
         this._nextButton.y = this.HEIGHT - 56;
         this._nextButton.addEventListener(MouseEvent.CLICK,this.nextStep);
         this._backButton = new PrevButton();
         this._backButton.x = this.WIDTH / 2;
         this._backButton.y = this.HEIGHT - 56;
         this._backButton.addEventListener(MouseEvent.CLICK,this.backStep);
         this._playButton = new PlayButton();
         this._playButton.x = this.WIDTH / 2;
         this._playButton.y = this.HEIGHT - 56;
         this._playButton.addEventListener(MouseEvent.CLICK,this.startStep);
         this._cbDontShow = new SOCheckBox();
         this._cbDontShow.defaultTextFormat = new TextFormat("Roboto Condensed Regular",14,16777215);
         this._cbDontShow.text = "Больше не показывать";
         this._cbDontShow.x = this.WIDTH - 20 - this._cbDontShow.width;
         this._cbDontShow.y = this.HEIGHT - 32 - this._cbDontShow.height;
         this._cbDontShow.addEventListener(MouseEvent.CLICK,this.onDontShowClick);
         this._closeButton = new SOButton();
         this._closeButton.label = "ЗАКРЫТЬ";
         this._closeButton.x = 20;
         this._closeButton.y = this.HEIGHT - 30 - this._closeButton.height;
         this._closeButton.addEventListener(MouseEvent.CLICK,this.onClickClose);
         addChild(this._background);
         addChild(this._picture);
         addChild(this._mainHelp);
         this._picture.addChild(this._step_title);
         addChild(this._descriprionBackground);
         addChild(this._descriprion);
         addChild(this._nextButton);
         addChild(this._backButton);
         addChild(this._playButton);
         addChild(this._cbDontShow);
         addChild(this._closeButton);
      }
      
      protected function help_ammo_visible(value:Boolean) : *
      {
         var btn:SOButton = null;
         this._title.visible = value;
         this._picture.visible = value;
         this._step_title.visible = value;
         this._nextButton.visible = value;
         this._backButton.visible = value;
         this._playButton.visible = value;
         this._cbDontShow.visible = value;
         this._descriprion.visible = value;
         this._descriprionBackground.visible = value;
         this._main_title.visible = !value;
         this._mainHelp.visible = !value;
         for each(btn in this._additionalButtons)
         {
            btn.visible = !value;
         }
      }
      
      protected function createAditbutton(name:String, local:String) : *
      {
         var bth:SOButton = new SOButton();
         bth.label = local;
         bth.x = 20;
         bth.y = this.HEIGHT - 40 - bth.height * (this._additionalButtons.length + 2);
         bth.width = 700;
         bth.visible = false;
         bth.name = name;
         bth.addEventListener(MouseEvent.CLICK,this.onClickReload);
         this._additionalButtons.push(bth);
         addChild(bth);
      }
      
      protected function onConfigRecieved(reciev_dict:Object) : *
      {
         var key:* = undefined;
         var i:* = undefined;
         var obj:Object = null;
         var url:URLRequest = null;
         this._contentDict = new Object();
         this._contentDict["local"] = new Object();
         this._main_title.text = reciev_dict.old.main_title;
         this._mainHelp.htmlText = reciev_dict.old.main_help;
         this._closeButton.label = reciev_dict.old.close;
         for(var j:int = 0; j < reciev_dict.old.additional_info_btn.length; j++)
         {
            this.createAditbutton(reciev_dict.old.additional_info_btn[j],reciev_dict.old.additional_info_local[j]);
         }
         for(key in reciev_dict)
         {
            this._contentDict[key] = new Array();
            this._contentDict["local"][key] = new Object();
            for(i in reciev_dict[key].data)
            {
               obj = new Object();
               this._contentDict["local"][key]["title"] = reciev_dict[key].title;
               this._contentDict["local"][key]["cb_label"] = reciev_dict[key].cb_label;
               this._contentDict["local"][key]["btn_label"] = reciev_dict[key].btn_label;
               obj["descriprion"] = reciev_dict[key].data[i].descriprion;
               obj["title"] = reciev_dict[key].data[i].title;
               obj["picture_path"] = reciev_dict[key].data[i].picture;
               obj["picture_loader"] = new Loader();
               this._contentDict[key].push(obj);
               url = new URLRequest(reciev_dict[key].data[i].picture);
               obj["picture_loader"].contentLoaderInfo.addEventListener(Event.COMPLETE,this.doneLoad(key,i,reciev_dict[key].data[i].picture));
               obj["picture_loader"].load(url);
            }
         }
         this.setStep(0);
         this.help_ammo_visible(this._title.visible);
      }
      
      protected function onHelpLink(event:TextEvent) : *
      {
         if(event.text == "RELOAD")
         {
            this.onShow({"type":"reload"});
         }
         else if(event.text.split(" ")[0] == "url")
         {
            ExternalInterface.call("open_url",{"url":event.text.split(" ")[1]});
         }
      }
      
      protected function onDontShowClick(event:MouseEvent) : *
      {
         ExternalInterface.call("dont_show",{
            "value":this._cbDontShow.checked,
            "help_type":this.currentHelp
         });
      }
      
      protected function onClickClose(event:MouseEvent) : *
      {
         ExternalInterface.call("onClose");
      }
      
      protected function onClickReload(event:MouseEvent) : *
      {
         this.onShow({"type":(event.target as SOButton).name});
      }
      
      protected function nextStep(event:Event) : *
      {
         this._current_step = Math.min(this._contentDict[this.currentHelp].length - 1,this._current_step + 1);
         if(this._current_step >= this._contentDict[this.currentHelp].length - 1)
         {
            this._timer.stop();
         }
         this.setStep(this._current_step);
      }
      
      protected function backStep(event:Event) : *
      {
         this._nextButton.enabled = true;
         this._current_step = Math.max(0,this._current_step - 1);
         this.setStep(this._current_step);
      }
      
      protected function startStep(event:MouseEvent) : *
      {
         if(!this._playButton.selected)
         {
            this._timer.start();
         }
         else
         {
            this._timer.reset();
            this._timer.stop();
         }
      }
      
      protected function updateBtnsEnable() : *
      {
         this._nextButton.enabled = this._current_step < this._contentDict[this.currentHelp].length - 1;
         this._backButton.enabled = this._current_step > 0;
      }
      
      protected function setStep(value:int) : *
      {
         this._current_step = value;
         if(this.currentHelp == "")
         {
            return;
         }
         if(this._contentDict[this.currentHelp] == null)
         {
            return;
         }
         this._descriprion.text = this._contentDict[this.currentHelp][this._current_step].descriprion;
         this._step_title.text = this._contentDict[this.currentHelp][this._current_step].title;
         this._title.text = this._contentDict["local"][this.currentHelp]["title"];
         this._cbDontShow.text = this._contentDict["local"][this.currentHelp]["cb_label"];
         this._closeButton.label = this._contentDict["local"][this.currentHelp]["btn_label"];
         this.clearPicture();
         if(this._contentDict[this.currentHelp][this._current_step].picture != null)
         {
            this._current_picture = this._contentDict[this.currentHelp][this._current_step].picture;
            this._picture.addChildAt(this._current_picture,0);
         }
         this.updateBtnsEnable();
      }
      
      protected function clearPicture() : *
      {
         if(this._picture.contains(this._current_picture))
         {
            this._picture.removeChild(this._current_picture);
         }
      }
      
      protected function doneLoad(help_type:String, index:int, picture:String) : *
      {
         return function(event:Event):void
         {
            var picture_loader:* = _contentDict[help_type][index]["picture_loader"];
            var loaded_picture:* = Bitmap(picture_loader.content);
            var ratio:* = _picture.width / _picture.height;
            loaded_picture.width = PICTURE_WIDTH;
            loaded_picture.height = PICTURE_HEIGHT;
            _contentDict[help_type][index]["picture"] = loaded_picture;
            if(_current_step == index)
            {
               clearPicture();
               _current_picture = _contentDict[help_type][index]["picture"];
               _picture.addChildAt(_current_picture,0);
            }
            addChild(_picture);
         };
      }
      
      protected function onShow(obj:*) : *
      {
         this.currentHelp = obj.type;
         this.visible = true;
         this.setStep(0);
         this._playButton.selected = true;
         this._timer.start();
         this.help_ammo_visible(true);
      }
      
      protected function onShowMainHelp(obj:*) : *
      {
         this.visible = true;
         this.help_ammo_visible(false);
      }
      
      protected function onHide() : *
      {
         this.visible = false;
         this.setStep(0);
         this._playButton.selected = false;
         this._timer.stop();
      }
      
      protected function onWindowStartDrag(e:Event) : void
      {
         stage.addEventListener(MouseEvent.MOUSE_UP,this.onWindowStopDrag,false,0,true);
         stage.addEventListener(MouseEvent.MIDDLE_MOUSE_UP,this.onWindowStopDrag,false,0,true);
         startDrag();
      }
      
      protected function onWindowStopDrag(e:Event) : void
      {
         stage.removeEventListener(MouseEvent.MOUSE_UP,this.onWindowStopDrag,false);
         stopDrag();
      }
   }
}

