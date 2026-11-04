package com
{
   import flash.display.MovieClip;
   import flash.events.Event;
   import flash.events.MouseEvent;
   import flash.utils.setTimeout;
   import scaleform.clik.controls.Button1;
   import scaleform.clik.controls.ButtonGroup;
   import scaleform.clik.core.UIComponent;
   import scaleform.clik.data.DataProvider;
   import scaleform.clik.data.ListDataSO;
   import scaleform.clik.events.ButtonEvent;
   import scaleform.clik.interfaces.IDataProvider;
   import scaleform.clik.utils.ConstrainMode;
   import scaleform.clik.utils.Constraints;
   import scaleform.clik.utils.Padding;
   
   public class TabsBar extends UIComponent
   {
      protected var _autoSize:String = "none";
      
      protected var _buttonWidth:Number = 0;
      
      protected var _dataProvider:IDataProvider;
      
      protected var _group:ButtonGroup;
      
      protected var _itemRenderer:String = "Button";
      
      protected var _itemRendererClass:Class;
      
      protected var _labelField:String = "label";
      
      protected var _labelFunction:Function;
      
      protected var _renderers:Array;
      
      protected var _spacing:Number = 0;
      
      protected var _selectedIndex:Number = -1;
      
      protected var _minTabSize:Number = 90;
      
      protected var _maxTabSize:Number = 200;
      
      public var _currentTabSize:Number = this._minTabSize;
      
      public var container:MovieClip;
      
      public var TabBarFirst:DefaultTileList;
      
      public var TabBarSec:DefaultTileList;
      
      public var Right:ScrlBtnR;
      
      public var Left:ScrlBtnR;
      
      public var Tabs:Array = new Array();
      
      public function TabsBar()
      {
         super();
      }
      
      override protected function initialize() : void
      {
         super.initialize();
      }
      
      public function invalidateTabs() : *
      {
         var i:* = undefined;
         for(i in this.Tabs)
         {
            this.changeSize();
         }
      }
      
      public function setNotification(i:Number, key:Boolean) : *
      {
         var s:String = "";
         if(key)
         {
            s = "notification";
         }
         else
         {
            s = "";
         }
         if(i >= this.TabBarFirst.dataProvider.length)
         {
            i -= this.TabBarFirst.dataProvider.length;
            this.TabBarSec.dataProvider[i].state = s;
            this.TabBarSec.invalidate();
         }
         else if(i < this.TabBarFirst.dataProvider.length && i >= 0)
         {
            this.TabBarFirst.dataProvider[i].state = s;
            this.TabBarFirst.invalidate();
         }
      }
      
      public function changeDataArray(i:Number, s:String) : void
      {
         if(i >= this.TabBarFirst._dataArray.length)
         {
            i -= this.TabBarFirst._dataArray.length;
            this.TabBarSec._dataArray[i].label = s;
            this.TabBarSec.invalidateData();
         }
         else if(i < this.TabBarFirst._dataArray.length && i >= 0)
         {
            this.TabBarFirst._dataArray[i].label = s;
            this.TabBarFirst.invalidateData();
         }
      }
      
      public function set autoSize(value:String) : void
      {
         if(value == this._autoSize)
         {
            return;
         }
         this.TabBarFirst.autoSize = value;
         this.TabBarSec.autoSize = value;
      }
      
      public function get selectedIndex() : int
      {
         return this._selectedIndex;
      }
      
      internal function getSelectedTab(e:ButtonEvent) : void
      {
         this._selectedIndex = e.target.index;
         this.setNotification(this._selectedIndex,false);
         this.TabBarSec.invalidateSize();
         dispatchEvent(new Event("SET_TAB"));
      }
      
      internal function getSelectedTab2(e:ButtonEvent) : void
      {
         this._selectedIndex = e.target.index + this.TabBarFirst.columnCount;
         this.setNotification(this._selectedIndex,false);
         this.TabBarFirst.invalidateSize();
         dispatchEvent(new Event("SET_TAB"));
      }
      
      public function TabSize(value:Number) : *
      {
         if(this._minTabSize <= value && value <= this._maxTabSize)
         {
            this._currentTabSize = value;
            this.TabBarFirst.columnWidth = value;
            this.TabBarSec.columnWidth = value;
            Object(root).MainChat.titleBtn.width = value + 18;
            this.TabBarFirst.width = (value + 0.25) * this.TabBarFirst.dataArray.length + 3;
            this.TabBarFirst.x = value + 20 + Object(root).MainChat.newBtn.width + 1;
            Object(root).MainChat.newBtn.x = Object(root).MainChat.titleBtn.width + 1;
         }
      }
      
      public function set selectedIndex(value:int) : void
      {
         if(value >= this.TabBarFirst._dataArray.length)
         {
            this._selectedIndex = value;
            value -= this.TabBarFirst._dataArray.length;
            this.TabBarFirst.selectedIndex = -1;
            this.TabBarSec.selectedIndex = value;
         }
         else if(value < this.TabBarFirst._dataArray.length && value >= 0)
         {
            this._selectedIndex = value;
            this.TabBarFirst.selectedIndex = value;
            this.TabBarSec.selectedIndex = -1;
         }
         else
         {
            this._selectedIndex = -1;
         }
      }
      
      public function getRendererAt(value:Number) : Button1
      {
         if(value >= 0 && value < this.TabBarFirst.dataArray.length)
         {
            return this.TabBarFirst.getRendererAt(value) as Button1;
         }
         if(value >= this.TabBarFirst.dataArray.length)
         {
            value -= this.TabBarFirst.dataArray.length;
            return this.TabBarSec.getRendererAt(value) as Button1;
         }
         return null;
      }
      
      public function dataProvider(dat:Array) : *
      {
         var Obj:Object = null;
         var tmp:Object = null;
         var j:* = undefined;
         for(var i:* = 0; i < dat.length; i++)
         {
            Obj = new Object();
            Obj.label = dat[i]["label"];
            if(String(dat[i].text) != "undefined:")
            {
               Obj.TEXT = dat[i].text;
            }
            else
            {
               Obj.TEXT = "";
            }
            Obj.setting = dat[i].chanalParam;
            Obj.user = dat[i].user;
            Obj.whisp = dat[i].whisp;
            Obj.defaultTab = dat[i].defaultTab;
            Obj.onlyEng = dat[i].onlyEng;
            if(Obj.whisp)
            {
               tmp = new Object();
               tmp.label = "@" + Obj.user;
               tmp.selected = true;
               tmp.id = -99;
               tmp.com = "@" + Obj.user;
               for(j in Object(root).MainChat.defaultChannals)
               {
                  if(Object(root).MainChat.defaultChannals[j].label == Object(root).MainChat.locale.WHISPER)
                  {
                     tmp.color = Object(root).MainChat.defaultChannals[j].color;
                  }
               }
               Obj.setting.unshift(tmp);
            }
            this.addTab(Obj.setting,Obj.label,Obj.TEXT,Obj.user,Obj.whisp,Obj.defaultTab,Obj.onlyEng);
         }
      }
      
      public function Drop() : *
      {
         this.Tabs = new Array();
         this.TabBarFirst.dateProvider = new DataProvider();
         this.TabBarFirst.dataArray = new Array();
         this.TabBarSec.dateProvider = new DataProvider();
         this.TabBarSec.dataArray = new Array();
         this.TabBarFirst.width = 10;
         this.TabBarSec.width = 10;
         this.width = 20;
      }
      
      override protected function preInitialize() : void
      {
         constraints = new Constraints(this,ConstrainMode.REFLOW);
      }
      
      override public function toString() : String
      {
         return "[TabBar " + name + "]";
      }
      
      override protected function configUI() : void
      {
         super.configUI();
         constraints.addElement("TabBarSec",this.TabBarSec,Constraints.RIGHT | Constraints.LEFT);
         constraints.addElement("Left",this.Left,Constraints.LEFT);
         constraints.addElement("Right",this.Right,Constraints.RIGHT);
         this.Left.addEventListener(MouseEvent.CLICK,this.ScrollLeft);
         this.Right.addEventListener(MouseEvent.CLICK,this.ScrollRight);
         this.TabBarFirst.addEventListener(MouseEvent.MOUSE_DOWN,this.onFind,false,0,true);
         this.TabBarSec.addEventListener(MouseEvent.MOUSE_DOWN,this.onFind,false,0,true);
         this.TabBarFirst.addEventListener(MouseEvent.RIGHT_CLICK,this.onContext,false,0,true);
         this.TabBarSec.addEventListener(MouseEvent.RIGHT_CLICK,this.onContext2,false,0,true);
         this.TabBarFirst.addEventListener(MouseEvent.MOUSE_UP,this.onFind1,false,0,true);
         this.TabBarSec.addEventListener(MouseEvent.MOUSE_UP,this.onFind1,false,0,true);
         this.TabBarFirst.addEventListener(ButtonEvent.CLICK,this.getSelectedTab);
         this.TabBarSec.addEventListener(ButtonEvent.CLICK,this.getSelectedTab2);
         this.TabBarFirst.columnWidth = this._currentTabSize;
         this.TabBarSec.columnWidth = this._currentTabSize;
         this.TabBarSec.addEventListener(Event.SCROLL,this.visibleScroll);
      }
      
      protected function onContext(e:MouseEvent) : *
      {
         dispatchEvent(new TabBarEvent(TabBarEvent.RIGHT_CLICK,e.target.index));
      }
      
      protected function onContext2(e:MouseEvent) : *
      {
         dispatchEvent(new TabBarEvent(TabBarEvent.RIGHT_CLICK,e.target.index + this.TabBarFirst.dataProvider.length));
      }
      
      public function resizeTab() : *
      {
         this.changeSize();
      }
      
      internal function tabDown() : *
      {
         if(this.TabBarSec._dataArray.length > 0)
         {
            this.TabBarFirst.addTab(-1,this.TabBarSec.dataArray[0]);
            this.TabBarSec.delTabI(0);
         }
      }
      
      internal function tabUp() : *
      {
         if(this.TabBarFirst._dataArray.length > 0)
         {
            this.TabBarSec.addTab(0,this.TabBarFirst.dataArray[this.TabBarFirst._dataArray.length - 1]);
            this.TabBarFirst.delTabI(this.TabBarFirst._dataArray.length - 1);
         }
      }
      
      internal function changeSize() : *
      {
         var size:Number = NaN;
         var w1:Number = this.TabBarSec.width / this._minTabSize;
         w1 = int(w1);
         if(this.TabBarFirst.dataArray.length > w1 - 1)
         {
            this.tabUp();
         }
         if(this.TabBarFirst.dataArray.length < w1 - 1)
         {
            this.tabDown();
         }
         if(this.TabBarSec.dataArray.length == 0)
         {
            size = this.TabBarSec.width / (this.TabBarFirst.dataArray.length + 1);
         }
         else
         {
            size = this.TabBarSec.width / w1;
         }
         this.TabSize(size - 0.5);
         this.visibleScroll(null);
      }
      
      internal function onFind(evt:MouseEvent) : void
      {
         evt.target.addEventListener(MouseEvent.MOUSE_OUT,this.onStartDrag);
      }
      
      internal function onFind1(evt:MouseEvent) : void
      {
         evt.target.removeEventListener(MouseEvent.MOUSE_OUT,this.onStartDrag);
      }
      
      internal function onStopDrag(evt:MouseEvent) : void
      {
         var object:* = evt.target;
         object.parent.removeEventListener(MouseEvent.MOUSE_UP,this.onStopDrag);
         object.parent.removeEventListener(MouseEvent.MOUSE_DOWN,this.onStartDrag);
         object.stopDrag();
      }
      
      internal function onStartDrag(evt:MouseEvent) : void
      {
         var tmp:Number = NaN;
         evt.target.removeEventListener(MouseEvent.MOUSE_OUT,this.onStartDrag);
         var object:* = evt.target;
         var newWin:Wind = new Wind();
         newWin.name = evt.target.label;
         newWin.x = Object(root).mouseX;
         newWin.y = Object(root).mouseY;
         newWin.title = String(evt.target.label);
         newWin.contentPadding = new Padding(35,20,20,20);
         if(evt.target.parent.parent.name == "TabBarFirst")
         {
            tmp = Number(evt.target.index);
         }
         if(evt.target.parent.parent.name == "TabBarSec")
         {
            tmp = evt.target.index + this.TabBarFirst.dataArray.length;
         }
         newWin.txt.htmlText = this.Tabs[tmp].TEXT;
         newWin.settings = this.Tabs[tmp].setting;
         newWin.user = this.Tabs[tmp].user;
         newWin.whisp = this.Tabs[tmp].whisp;
         newWin.defaultTab = this.Tabs[tmp].defaultTab;
         newWin.onlyEng = this.Tabs[tmp].onlyEng;
         evt.target.parent.parent.delTabI(evt.target.index);
         this.Tabs.splice(tmp,1);
         this.changeSize();
         newWin.locale = Object(root).MainChat.locale;
         Object(root).addChild(newWin);
         Object(root).MainChat.AllMsg();
         newWin.newWindow();
         setTimeout(newWin.initRamka,10);
         newWin.startDrag();
         this.removeEventListener(MouseEvent.MOUSE_DOWN,this.onStartDrag);
         Object(root).MainChat.closeOptions();
         newWin.fontSized = -1;
         this.selectedIndex = -1;
         this.TabBarSec.selectedIndex = -1;
         this.TabBarFirst.selectedIndex = -1;
      }
      
      public function deleteTab(value:Number) : *
      {
         if(value >= this.TabBarFirst._dataArray.length)
         {
            this.Tabs.splice(value,1);
            value -= this.TabBarFirst._dataArray.length;
            this.TabBarSec.delTabI(value);
         }
         else if(value < this.TabBarFirst._dataArray.length && value >= 0)
         {
            this.TabBarFirst.delTabI(value);
            this.Tabs.splice(value,1);
         }
         this.changeSize();
      }
      
      public function visibleScroll(e:Event) : *
      {
         if(this.TabBarSec.dataArray.length > this.TabBarSec.columnCount)
         {
            this.Right.visible = true;
            this.Left.visible = true;
         }
         else
         {
            this.Right.visible = false;
            this.Left.visible = false;
         }
         if(this.TabBarSec.scrollPosition !== 0)
         {
            this.Left.enabled = true;
         }
         else
         {
            this.Left.enabled = false;
         }
         if(this.TabBarSec.scrollPosition + this.TabBarSec.columnCount >= this.TabBarSec.dataArray.length)
         {
            this.Right.enabled = false;
         }
         else
         {
            this.Right.enabled = true;
         }
      }
      
      public function validateNotif() : *
      {
         var R:* = undefined;
         var L:Boolean = false;
         for(var i:* = this.TabBarSec.scrollPosition + this.TabBarSec.columnCount; i < this.TabBarSec.dataProvider.length; i++)
         {
            if(this.TabBarSec.dataProvider[i].state == "notification")
            {
               R = true;
            }
         }
         for(var j:* = 0; j < this.TabBarSec.scrollPosition; j++)
         {
            if(this.TabBarSec.dataProvider[j].state == "notification")
            {
               L = true;
            }
         }
         var li:ListDataSO = new ListDataSO(0,"Empty",false,"notification");
         var notli:ListDataSO = new ListDataSO(0,"Empty",false,"");
         if(R)
         {
            this.Right.setListData(li);
         }
         else
         {
            this.Right.setListData(notli);
         }
         if(L)
         {
            this.Left.setListData(li);
         }
         else
         {
            this.Left.setListData(notli);
         }
         this.Right.invalidate();
         this.Left.invalidate();
         this.TabBarSec.invalidate();
      }
      
      public function ScrollLeft(e:MouseEvent) : *
      {
         --this.TabBarSec.scrollPosition;
         this.visibleScroll(null);
         this.validateNotif();
      }
      
      public function ScrollRight(e:MouseEvent) : *
      {
         this.TabBarSec.scrollPosition += 1;
         this.visibleScroll(null);
         this.validateNotif();
      }
      
      public function copyTab(ind:Number) : *
      {
         var i:* = undefined;
         var zz:Object = null;
         var Obj:Object = new Object();
         Obj.label = this.Tabs[ind].label;
         Obj.TEXT = this.Tabs[ind].TEXT;
         var arr:Array = new Array();
         for(i in this.Tabs[ind].setting)
         {
            zz = new Object();
            zz.id = this.Tabs[ind].setting[i].id;
            zz.label = this.Tabs[ind].setting[i].label;
            zz.com = this.Tabs[ind].setting[i].com;
            zz.selected = this.Tabs[ind].setting[i].selected;
            zz.color = this.Tabs[ind].setting[i].color;
            arr.push(zz);
         }
         Obj.setting = arr;
         Obj.user = this.Tabs[ind].user;
         Obj.whisp = this.Tabs[ind].whisp;
         Obj.defaultTab = this.Tabs[ind].defaultTab;
         Obj.onlyEng = this.Tabs[ind].onlyEng;
         if(ind >= 0)
         {
            if(ind < this.TabBarFirst.dataArray.length)
            {
               this.TabBarFirst.addTab(ind,{
                  "label":Obj.label,
                  "state":""
               });
               this.Tabs.splice(ind,0,Obj);
            }
            else
            {
               this.Tabs.splice(ind,0,Obj);
               ind -= this.TabBarFirst.dataArray.length;
               this.TabBarSec.addTab(ind,{
                  "label":Obj.label,
                  "state":""
               });
            }
         }
         this.changeSize();
      }
      
      public function addTab(arr:Array, name:String = "NoName", txt:String = "", user:String = "", whisp:Boolean = false, defaultTab:String = "", onlyEng:Boolean = false) : *
      {
         var Obj:Object = new Object();
         Obj.label = name;
         Obj.TEXT = txt;
         Obj.setting = arr;
         Obj.user = user;
         Obj.whisp = whisp;
         Obj.defaultTab = defaultTab;
         Obj.onlyEng = onlyEng;
         if(this.TabBarSec._dataArray.length > 0)
         {
            this.TabBarSec.addTab(-1,{
               "label":name,
               "state":""
            });
            this.Tabs.splice(this.Tabs.length,0,Obj);
         }
         else
         {
            this.TabBarFirst.addTab(this.TabBarFirst._dataArray.length,{
               "label":name,
               "state":""
            });
            this.Tabs.splice(this.TabBarFirst._dataArray.length,0,Obj);
         }
         this.changeSize();
      }
      
      public function addDefTab(user:String = "", MSG:Object = null) : *
      {
         var j:* = undefined;
         var str:* = null;
         var zz:Object = null;
         var Obj:Object = new Object();
         Obj.label = "@" + user;
         Obj.TEXT = "";
         Obj.defaultTab = "";
         Obj.onlyEng = false;
         Obj.setting = new Array();
         var Tarr:Array = new Array();
         Tarr = Object(root).MainChat.defaultChannals;
         for(var Z:* = 0; Z < Tarr.length; Z++)
         {
            zz = new Object();
            zz = Tarr[Z];
            Obj.setting.push(zz);
         }
         var tmp:Object = new Object();
         tmp.label = "@" + user;
         tmp.selected = true;
         tmp.id = -99;
         tmp.com = "@" + user;
         for(j in Object(root).MainChat.defaultChannals)
         {
            if(Object(root).MainChat.defaultChannals[j].label == Object(root).MainChat.locale.WHISPER)
            {
               tmp.color = Object(root).MainChat.defaultChannals[j].color;
               break;
            }
         }
         Obj.setting.unshift(tmp);
         Obj.user = user;
         Obj.whisp = true;
         this.addTab(Obj.setting,Obj.label,Obj.TEXT,Obj.user,Obj.whisp,Obj.defaultTab,Obj.onlyEng);
         this.changeSize();
         str = "";
         if(Obj.setting[0].color != null)
         {
            str = "<font color=\"#" + Obj.setting[0].color.toString(16) + "\">";
         }
         else
         {
            str = "<font color=\"#9a85a7\">";
         }
         if(Object(root).MainChat.ShowTime)
         {
            str = str + "[" + MSG.tme + "]";
         }
         str = str + "<a href=\"event:" + MSG.usr + "\">[" + MSG.usr + "]</a>:";
         str = str + MSG.msg + "</font>\n";
         this.Tabs[this.Tabs.length - 1].TEXT += str;
         this.setNotification(this.Tabs.length - 1,true);
         this.validateNotif();
      }
      
      override protected function draw() : void
      {
         constraints.update(_width,_height);
         this.resizeTab();
      }
   }
}

