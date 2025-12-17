<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:msxsl="urn:schemas-microsoft-com:xslt" exclude-result-prefixes="msxsl">

  <!--xsl:output method="html" doctype-public="-//W3C//DTD XHTML 1.0 Strict//EN" indent="yes"/-->
  <xsl:output method="html"/>
  <xsl:param name="DisplayInApp"/>
  
  
  <xsl:template match="/ArrayOfPrintableCheckItem">
   
        <div class="Section1">

          <!-- <xsl:apply-templates select="PrintableCheckItem[number(ViolationCount) > 0]" /> -->
          <xsl:apply-templates select="PrintableCheckItem[count(./Violations/Violation[Hidden/text() = 'false'])>0]" />
          
        </div>
      
  </xsl:template>

  <xsl:template name="PrintSeverityHeader">
    <xsl:if test="$DisplayInApp != 'true'">
      <xsl:choose>
        <xsl:when test="position() = 1">
          
          <xsl:call-template name="TransformSeverityToDisplayText">
            <xsl:with-param name="SeverityText" select="Severity/text()"/>
          </xsl:call-template>
          </xsl:when>
        <xsl:otherwise>

          <xsl:variable name="CurrentSeverity" select="Severity"/>
          <xsl:variable name="PreviousSeverity" select="preceding-sibling::*[1]/Severity" />
          
            <xsl:if test="$CurrentSeverity != $PreviousSeverity">
            <xsl:call-template name="TransformSeverityToDisplayText">
              <xsl:with-param name="SeverityText" select="Severity/text()"/>
            </xsl:call-template>              
          </xsl:if>
          

        </xsl:otherwise>
      </xsl:choose>
    

    </xsl:if>
  </xsl:template>

  <xsl:template name="InsertPageBreak">
    <p style="page-break-before:always;color:white">Insert page break here.</p>
  </xsl:template>

  <xsl:template name="TransformSeverityToDisplayText">
    <xsl:param name="SeverityText" select="Default"/>

  </xsl:template>

  <xsl:template name="InsertCheckItemDeleteLink">
    <xsl:if test="$DisplayInApp = 'true'">
      <div style="float: right;" align="right">
        <xsl:element name='a'>
          <xsl:attribute name='href'>
            mspcdelci:<xsl:value-of select='UniqueId'/>
          </xsl:attribute>
          <!--img src="res://PracticesChecker.exe/23/104" style="border-style: none"/-->
          <font color='white' face='webdings' style="text-decoration:none" onmouseover="this.style.color='red';this.style.fontWeight='bold'" onmouseout="this.style.color='white';this.style.fontWeight='normal'">r</font>
        </xsl:element>
      </div>
    </xsl:if>
  </xsl:template>

  <xsl:template name='InsertCheckItemEditLink'>

    <xsl:if test="$DisplayInApp = 'true'">

      <div style="float: right;" align="right">

        <xsl:element name="a">
          <xsl:attribute name="href">
            mspceditci:<xsl:value-of select="UniqueId" />
          </xsl:attribute>
          <font color='white'>Edit</font>
        </xsl:element>
      </div>
    </xsl:if>
  </xsl:template>

  <xsl:template name='InsertCheckItemOptionSpacer'>
    <xsl:if test="$DisplayInApp = 'true'">
      <div style="float: right;" align="right">
          <font color="white">&#xA0;&#xA0;|&#xA0;&#xA0;</font>
      </div>
    </xsl:if>
  </xsl:template>

  <xsl:template name='InsertSaveLink'>

    <xsl:if test="$DisplayInApp = 'true'">

      <div style="margin:3px;float:right;visibility:hidden" align="right" >

        <xsl:element name="a">
          <xsl:attribute name="href">
            mspcsavcom:<xsl:value-of select="UniqueId" />
          </xsl:attribute>
          <img src="res://PracticesChecker.exe/23/102" style="border-style: none"/>
        </xsl:element>
      </div>
    </xsl:if>
  </xsl:template>

  <xsl:template name='InsertCancelLink'>

    <xsl:if test="$DisplayInApp = 'true'">

      <div style="margin:3px;float:right;visibility:hidden" align="right" >

        <xsl:element name="a">
          <xsl:attribute name="href">
            mspccancom:<xsl:value-of select="UniqueId" />
          </xsl:attribute>
          <img src="res://PracticesChecker.exe/23/103" style="border-style: none"/>
          <!--font color='black'>Cancel</font-->
        </xsl:element>
      </div>
    </xsl:if>
  </xsl:template>

  <xsl:template match="PrintableCheckItem[EscapeFormatting/text() = 'true']">
    <!-- <p style="page-break-before: always">BLAH BLAH BLAH</p> -->

    <xsl:call-template name="PrintSeverityHeader" />

    <table class="MsoNormalTable" border="0" cellspacing="0" cellpadding="0" style='border-collapse:collapse'>
      <tr>
        <td  valign='top' style='border:solid #4F81BD 1.0pt;background:#4F81BD;padding:0in 5.4pt 0in 5.4pt'>
          <xsl:call-template name='InsertCheckItemDeleteLink'/>
          <!--xsl:call-template name='InsertCheckItemOptionSpacer'/-->
          <!--xsl:call-template name='InsertCheckItemEditLink'/-->
          <p class='MsoNormal' style='margin-bottom:0in;margin-bottom:.0001pt;line-height:normal'>
            <b>
              <span style='color:white'>
                <xsl:element name="a">
                  <xsl:attribute name="name">
                    <xsl:value-of select="ID"/>
                  </xsl:attribute>
                  <xsl:value-of select='Title'/>
                </xsl:element>

              </span>
            </b>
          </p>
        </td>
      </tr>
      <tr>
        <td  valign='top' style='border:solid #4F81BD 1.0pt;border-top:none;background:#DBE5F1;padding:0in 5.4pt 0in 5.4pt'>
          <p class='MsoNormal' style='margin-bottom:0in;margin-bottom:.0001pt;line-height:normal'>
            <b>Severity</b>
          </p>
        </td>
      </tr>
      <tr>
        <td  valign='top' style='border:solid #4F81BD 1.0pt;border-top:none;background:white;padding:0in 5.4pt 0in 5.4pt'>
          <p class='MsoNormal' style='margin-bottom:0in;margin-bottom:.0001pt;line-height:normal'>
            <b>
              <span style='color:#F79646'>
                <xsl:value-of select='Severity'/>
              </span>
            </b>
          </p>
        </td>
      </tr>
      <tr>
        <td  valign='top' style='border:solid #4F81BD 1.0pt;border-top:none;background:#DBE5F1;padding:0in 5.4pt 0in 5.4pt'>
          <p class='MsoNormal' style='margin-bottom:0in;margin-bottom:.0001pt;line-height:normal'>
            <b>Category</b>
          </p>
        </td>
      </tr>
      <tr>
        <td  valign='top' style='border:solid #4F81BD 1.0pt;border-top:none;padding:0in 5.4pt 0in 5.4pt'>
          <p class='MsoNormal' style='margin-bottom:0in;margin-bottom:.0001pt;line-height:normal'>
            <xsl:value-of select='Category'/>
          </p>
        </td>
      </tr>
      <tr>
        <td  valign='top' style='border:solid #4F81BD 1.0pt;border-top:none;background:#DBE5F1;padding:0in 5.4pt 0in 5.4pt'>
          <p class='MsoNormal' style='margin-bottom:0in;margin-bottom:.0001pt;line-height:normal'>
            <b>Description</b>
          </p>
        </td>
      </tr>
      <tr>
        <td  valign='top' style='border:solid #4F81BD 1.0pt;border-top:none;padding:0in 5.4pt 0in 5.4pt'>
          <p class='MsoNormal' style='margin-bottom:0in;margin-bottom:.0001pt;line-height:normal'>
            <xsl:value-of select='Description' />
          </p>
        </td>
      </tr>
      <tr>
        <td  valign='top' style='border:solid #4F81BD 1.0pt;border-top:none;background:#DBE5F1;padding:0in 5.4pt 0in 5.4pt'>
          <xsl:call-template name='InsertSaveLink'/>
          <xsl:call-template name='InsertCancelLink'/>
          <p class='MsoNormal' style='margin-bottom:0in;margin-bottom:.0001pt;line-height:normal'>
            <b>Guideline</b>
          </p>
        </td>
      </tr>
      <tr>
        <td  valign='top' style='border:solid #4F81BD 1.0pt;border-top:none;padding:0in 5.4pt 0in 5.4pt'>
          <div class='MsoNormal' contentEditable='true' editRegion='Guideline' dirty='false' onkeyup='pcGuideline_OnKeyUp(this)' onkeydown='pcGuideline_OnKeyDown(this)' >
            <xsl:attribute name="uniqueId">
              <xsl:value-of select="UniqueId" />
            </xsl:attribute>

            <xsl:value-of select='Content' />
          </div>
        </td>
      </tr>
      <tr>
        <td  valign='top' style='border:solid #4F81BD 1.0pt;border-top:none;background:#DBE5F1;padding:0in 5.4pt 0in 5.4pt'>
          <p class='MsoNormal' style='margin-bottom:0in;margin-bottom:.0001pt;line-height:normal'>
            <b>References</b>
          </p>
        </td>
      </tr>
      <tr>
        <td  valign='top' style='border:solid #4F81BD 1.0pt;border-top:none;padding:0in 5.4pt 0in 5.4pt'>
          <p class='MsoNormal' style='margin-bottom:0in;margin-bottom:.0001pt;line-height:normal'>
            <xsl:apply-templates select='References'/>
          </p>
        </td>
      </tr>
      <tr>
        <td  valign="top" style='border:solid #4F81BD 1.0pt;border-top:none;background:#DBE5F1;padding:0in 5.4pt 0in 5.4pt'>
          <xsl:call-template name='InsertSaveLink'/>
          <xsl:call-template name='InsertCancelLink'/>          
          <p class='MsoNormal' style='margin-bottom:0in;margin-bottom:.0001pt;line-height:normal'>
            <b>Comments</b>
          </p>
        </td>
      </tr>
      <tr>
        <td  valign='top' style='border:solid #4F81BD 1.0pt;border-top:none;padding:0in 5.4pt 0in 5.4pt'>
          <div class='MsoNormal' contentEditable='true' editRegion='Comments' dirty='false' onkeyup='pcComments_OnKeyUp(this)' onkeydown='pcComments_OnKeyDown(this)' >
            <xsl:attribute name="uniqueId">
              <xsl:value-of select="UniqueId" />
            </xsl:attribute>

            <xsl:apply-templates select='Comments' />
          </div>
        </td>
      </tr>

      <xsl:apply-templates select='Violations' />
    </table>
    <br/>
    <br/>
    <xsl:call-template name='InsertPageBreak'/>

  </xsl:template>

  <xsl:template match="PrintableCheckItem/Comments/string[ancestor::PrintableCheckItem/EscapeFormatting/text() = 'true']">

    <p class='MsoNormal' style='margin-bottom:0in;margin-bottom:.0001pt;line-height:normal'>
      <xsl:value-of select='.'/>
    </p>

  </xsl:template>

  <xsl:template match="PrintableCheckItem/Comments/string[ancestor::PrintableCheckItem/EscapeFormatting/text() = 'false']">

    <p class='MsoNormal' style='margin-bottom:0in;margin-bottom:.0001pt;line-height:normal'>
      <xsl:value-of select='.' disable-output-escaping='yes'/>
    </p>

  </xsl:template>
  
  <xsl:template match="Violation/Comments"><div class='violationComment'><xsl:value-of select='.'/></div></xsl:template>
  
  <xsl:template match="PrintableCheckItem[EscapeFormatting/text() = 'false']">
    <!-- <p style="page-break-before: always">BLAH BLAH BLAH</p> -->

    <xsl:call-template name="PrintSeverityHeader" />

    <table class="MsoNormalTable" border="0" cellspacing="0" cellpadding="0" style='border-collapse:collapse'>
      <tr>
        <td  valign='top' style='border:solid #4F81BD 1.0pt;background:#4F81BD;padding:0in 5.4pt 0in 5.4pt'>
          <xsl:call-template name='InsertCheckItemDeleteLink'/>
          <!--xsl:call-template name='InsertCheckItemOptionSpacer'/-->
          <!--xsl:call-template name='InsertCheckItemEditLink'/-->
          <p class='MsoNormal' style='margin-bottom:0in;margin-bottom:.0001pt;line-height:normal'>
            <b>
              <span style='color:white'>
                <xsl:element name="a">
                  <xsl:attribute name="name">
                    <xsl:value-of select="ID"/>
                  </xsl:attribute>
                  <xsl:value-of select='Title'/>
                </xsl:element>
              </span>
            </b>
          </p>
        </td>
      </tr>
      <tr>
        <td  valign='top' style='border:solid #4F81BD 1.0pt;border-top:none;background:#DBE5F1;padding:0in 5.4pt 0in 5.4pt'>
          <p class='MsoNormal' style='margin-bottom:0in;margin-bottom:.0001pt;line-height:normal'>
            <b>Severity</b>
          </p>
        </td>
      </tr>
      <tr>
        <td  valign='top' style='border:solid #4F81BD 1.0pt;border-top:none;background:white;padding:0in 5.4pt 0in 5.4pt'>
          <p class='MsoNormal' style='margin-bottom:0in;margin-bottom:.0001pt;line-height:normal'>
            <b>
              <span style='color:#F79646'>
                <xsl:value-of select='Severity'/>
              </span>
            </b>
          </p>
        </td>
      </tr>
      <tr>
        <td  valign='top' style='border:solid #4F81BD 1.0pt;border-top:none;background:#DBE5F1;padding:0in 5.4pt 0in 5.4pt'>
          <p class='MsoNormal' style='margin-bottom:0in;margin-bottom:.0001pt;line-height:normal'>
            <b>Category</b>
          </p>
        </td>
      </tr>
      <tr>
        <td  valign='top' style='border:solid #4F81BD 1.0pt;border-top:none;padding:0in 5.4pt 0in 5.4pt'>
          <p class='MsoNormal' style='margin-bottom:0in;margin-bottom:.0001pt;line-height:normal'>
            <xsl:value-of select='Category'/>
          </p>
        </td>
      </tr>
      <tr>
        <td  valign='top' style='border:solid #4F81BD 1.0pt;border-top:none;background:#DBE5F1;padding:0in 5.4pt 0in 5.4pt'>
          <p class='MsoNormal' style='margin-bottom:0in;margin-bottom:.0001pt;line-height:normal'>
            <b>Description</b>
          </p>
        </td>
      </tr>
      <tr>
        <td  valign='top' style='border:solid #4F81BD 1.0pt;border-top:none;padding:0in 5.4pt 0in 5.4pt'>
          <p class='MsoNormal' style='margin-bottom:0in;margin-bottom:.0001pt;line-height:normal'>
            <xsl:value-of select='Description' disable-output-escaping='yes'/>
          </p>
        </td>
      </tr>
      <tr>
        <td  valign='top' style='border:solid #4F81BD 1.0pt;border-top:none;background:#DBE5F1;padding:0in 5.4pt 0in 5.4pt'>
          <xsl:call-template name='InsertSaveLink'/>
          <xsl:call-template name='InsertCancelLink'/>
          <p class='MsoNormal' style='margin-bottom:0in;margin-bottom:.0001pt;line-height:normal'>
            <b>Guideline</b>
          </p>
        </td>
      </tr>
      <tr>
        <td  valign='top' style='border:solid #4F81BD 1.0pt;border-top:none;padding:0in 5.4pt 0in 5.4pt'>
          <div class='MsoNormal' contentEditable='true' editRegion='Guideline' dirty='false' onkeyup='pcGuideline_OnKeyUp(this)' onkeydown='pcGuideline_OnKeyDown(this)' >
            <xsl:attribute name="uniqueId">
              <xsl:value-of select="UniqueId" />
            </xsl:attribute>

            <xsl:value-of select='Content' disable-output-escaping='yes' />            
          </div>
          <!--xsl:value-of select='Content' disable-output-escaping='yes' /-->

        </td>
      </tr>      
      <tr>
        <td  valign='top' style='border:solid #4F81BD 1.0pt;border-top:none;background:#DBE5F1;padding:0in 5.4pt 0in 5.4pt'>
          <p class='MsoNormal' style='margin-bottom:0in;margin-bottom:.0001pt;line-height:normal'>
            <b>References</b>
          </p>
        </td>
      </tr>
      <tr>
        <td  valign='top' style='border:solid #4F81BD 1.0pt;border-top:none;padding:0in 5.4pt 0in 5.4pt'>
          <p class='MsoNormal' style='margin-bottom:0in;margin-bottom:.0001pt;line-height:normal'>
            <xsl:apply-templates select='References'/>
          </p>
        </td>
      </tr>
      <tr>
        <td  valign="top" style='border:solid #4F81BD 1.0pt;border-top:none;background:#DBE5F1;padding:0in 5.4pt 0in 5.4pt'>
          <xsl:call-template name='InsertSaveLink'/>
          <xsl:call-template name='InsertCancelLink'/>          
          <p class='MsoNormal' style='margin-bottom:0in;margin-bottom:.0001pt;line-height:normal'>
            <b>Comments</b>
          </p>
          
        </td>
      </tr>
      <tr>
        <td  valign='top' style='border:solid #4F81BD 1.0pt;border-top:none;padding:0in 5.4pt 0in 5.4pt'>
          <div class='MsoNormal' contentEditable='true' editRegion='Comments' dirty='false' onkeyup='pcComments_OnKeyUp(this)' onkeydown='pcComments_OnKeyDown(this)'>
            <xsl:attribute name="uniqueId">
              <xsl:value-of select="UniqueId" />
            </xsl:attribute>
            
          <xsl:apply-templates select='Comments' />
          </div>
        </td>
      </tr>

      <xsl:apply-templates select='Violations' />
    </table>
    <br/>
    <br/>
    <xsl:call-template name='InsertPageBreak'/>
  </xsl:template>

  <xsl:template match='Violations'>
    <!--xsl:if test='number(ancestor::PrintableCheckItem/ViolationCount/text())>0'-->
    <xsl:if test='count(./Violation)>0'>

      <tr>
        <td  valign='top' style='border:solid #4F81BD 1.0pt;border-top:none;background:#DBE5F1;padding:0in 5.4pt 0in 5.4pt'>
          <p class='MsoNormal' style='margin-bottom:0in;margin-bottom:.0001pt;line-height:normal'>
            <b>
              Violations [<span id='violationCount'><xsl:value-of select="count(Violation[Hidden/text() = 'false'])"/></span>]
            </b>
          </p>
        </td>
      </tr>
      <xsl:apply-templates select="Violation[Hidden/text() = 'false']" />

    </xsl:if>
  </xsl:template>


  <xsl:template match='Violation'>
    <tr>
      <td  valign='top' style='border:solid #4F81BD 1.0pt;border-top:none;padding:0in 0pt 0in 5.4pt'>
        <xsl:if test="$DisplayInApp = 'true'">
          <div style="float: right;" align="right">
            <xsl:element name='a'>
              <xsl:attribute name='href'>
                mspcdelviol:<xsl:value-of select='UniqueId'/>
              </xsl:attribute>
              <font color='#4F81BD' face='webdings' style="text-decoration:none" onmouseover="this.style.color='red';this.style.fontWeight='bold'" onmouseout="this.style.color='#4F81BD';this.style.fontWeight='normal'">r</font>
            </xsl:element>

          </div>
        </xsl:if>
        
        <xsl:if test='string-length(Module/text())>0'>
          <p class='MsoNormal' style='margin-bottom:0in;margin-bottom:.0001pt;line-height:normal'>
            <b>Module: </b>
            <xsl:value-of select='Module'/>
          </p>
        </xsl:if>

        <xsl:if test='string-length(Type/text())>0 and Type/text()!="."' >
          <p class='MsoNormal' style='margin-bottom:0in;margin-bottom:.0001pt;line-height:normal'>
            <b>Type: </b>
            <!-- <xsl:value-of select='Type'/>-->


            <xsl:choose>
              <xsl:when test="$DisplayInApp = 'true'">

                <xsl:element name='a'>
                  <xsl:attribute name='href'>
                    <xsl:value-of select='FullTypeName'/>
                  </xsl:attribute>
                  <xsl:value-of select='Type'/>
                </xsl:element>

              </xsl:when>
              <xsl:otherwise>
                <xsl:value-of select='Type'/>
              </xsl:otherwise>
            </xsl:choose>
          </p>
        </xsl:if>

        <xsl:if test='string-length(Member/text())>0'>
          <p class='MsoNormal' style='margin-bottom:0in;margin-bottom:.0001pt;line-height:normal'>
            <b>Member: </b>
            <xsl:choose>
              <xsl:when test="$DisplayInApp = 'true'">

                <xsl:element name='a'>
                  <xsl:attribute name='href'>
                    <xsl:value-of select='FullMemberName'/>
                  </xsl:attribute>
                  <xsl:value-of select='Member'/>
                </xsl:element>

              </xsl:when>
              <xsl:otherwise>
                <xsl:value-of select='Member'/>
              </xsl:otherwise>
            </xsl:choose>
          </p>
        </xsl:if>

        <xsl:if test='string-length(CStatement/text())>0'>
          <p class='MsoNormal' style='margin-bottom:0in;margin-bottom:.0001pt;line-height:normal'>
            <b>Statement: </b>
            <xsl:value-of select='CStatement' disable-output-escaping='no'/>
          </p>
        </xsl:if>

        <xsl:if test='string-length(Source/text())>0'>
          <p class='MsoNormal' style='margin-bottom:0in;margin-bottom:.0001pt;line-height:normal'>
            <b>Source: </b>
            <xsl:value-of select='Source'/>
          </p>
        </xsl:if>

        <xsl:if test='string-length(Line/text())>0'>
          <xsl:if test='number(Line)> 0'>
            <p class='MsoNormal' style='margin-bottom:0in;margin-bottom:.0001pt;line-height:normal'>
              <b>Line: </b>
              <xsl:value-of select='Line'/>
            </p>
          </xsl:if>
        </xsl:if>
        <xsl:if test='string-length(Comments/text())>0'>
          <p class='violationComment' style='margin-bottom:0in;margin-bottom:.0001pt;line-height:normal'>
            <b>Comments: </b>
            <xsl:value-of select='Comments/string' />
          </p>
        </xsl:if>
      </td>
    </tr>
  </xsl:template>


  <xsl:template match="References/string[ancestor::PrintableCheckItem/EscapeFormatting/text() = 'true']" >

    <xsl:element name='a'>
      <xsl:attribute name='href'>
        <xsl:value-of select="."/>
      </xsl:attribute>
      <xsl:value-of select="."/>
    </xsl:element>

    <br/>
  </xsl:template>

  <xsl:template match="References/string[ancestor::PrintableCheckItem/EscapeFormatting/text() = 'false']" >
    <xsl:variable name='EscapeHTML' select='ancestor::PrintableCheckItem/ViolationCount/text()' />

    <!-- ancestor::PrintableCheckItem/ViolationCount -->
    <xsl:value-of select='.' disable-output-escaping='yes'/>

  </xsl:template>
</xsl:stylesheet>
