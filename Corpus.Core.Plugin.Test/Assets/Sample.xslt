<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:tei="http://www.tei-c.org/ns/1.0" version="1.0">
  <xsl:output method="html" omit-xml-declaration="yes"/>

  <!-- getPnType -->
  <xsl:template name="getPnType">
    <xsl:param name="type"/>
    <xsl:choose>
      <xsl:when test="$type = 'fn'">female name</xsl:when>
      <xsl:when test="$type = 'mn'">male name</xsl:when>
      <xsl:when test="$type = 's'">surname</xsl:when>
    </xsl:choose>
  </xsl:template>

  <!-- teiHeader -->
  <xsl:template match="tei:teiHeader"></xsl:template>

  <!-- p -->
  <xsl:template match="tei:p">
    <p>
      <xsl:if test="@rend">
        <xsl:attribute name="class">
          pa-<xsl:value-of select="@rend"/>
        </xsl:attribute>
      </xsl:if>
      <xsl:apply-templates/>
    </p>
  </xsl:template>

  <!-- choice -->
  <xsl:template match="tei:choice[tei:expan]">
    <span>
      <xsl:attribute name="title">
        <xsl:value-of select="tei:expan/text()"/>
      </xsl:attribute>
      <xsl:apply-templates/>
    </span>
  </xsl:template>

  <!-- abbr -->
  <xsl:template match="tei:abbr">
    <span class="abbr">
      <xsl:apply-templates/>
    </span>
  </xsl:template>

  <!-- expan -->
  <xsl:template match="tei:expan"></xsl:template>

  <!-- address -->
  <xsl:template match="tei:address">
    <span class="address" title="address">
      <xsl:apply-templates/>
    </span>
  </xsl:template>

  <!-- date -->
  <xsl:template match="tei:date">
    <span class="date" title="date">
      <xsl:apply-templates/>
    </span>
  </xsl:template>

  <!-- email -->
  <xsl:template match="tei:email">
    <span class="email" title="email">
      <xsl:apply-templates/>
    </span>
  </xsl:template>

  <!-- foreign -->
  <xsl:template match="tei:foreign">
    <span class="foreign">
      <xsl:attribute name="title">
        <xsl:value-of select="@xml:lang"/>
      </xsl:attribute>
      <xsl:apply-templates/>
    </span>
  </xsl:template>

  <!-- hi -->
  <xsl:template match="tei:hi">
    <span>
      <xsl:attribute name="class">
        hi-<xsl:value-of select="@rend"/>
      </xsl:attribute>
      <xsl:apply-templates/>
    </span>
  </xsl:template>

  <!-- num -->
  <xsl:template match="tei:num">
    <span class="num" title="number">
      <xsl:apply-templates/>
    </span>
  </xsl:template>

  <!-- orgName -->
  <xsl:template match="tei:orgName">
    <span class="juridic-name" title="juridic name">
      <xsl:apply-templates />
    </span>
  </xsl:template>

  <!-- persName -->
  <xsl:template match="tei:persName">
    <span class="pers-name">
      <xsl:attribute name="title">
        <xsl:call-template name="getPnType">
          <xsl:with-param name="type" select="@type"/>
        </xsl:call-template>
      </xsl:attribute>
      <xsl:apply-templates />
    </span>
  </xsl:template>

  <!-- placeName -->
  <xsl:template match="tei:placeName">
    <span class="place-name" title="place name">
      <xsl:apply-templates />
    </span>
  </xsl:template>

  <!-- catch-all -->
  <xsl:template match="*">
    <xsl:message terminate="no">
      WARNING: Unmatched element: <xsl:value-of select="name()"/>
    </xsl:message>
    <xsl:apply-templates/>
  </xsl:template>

  <!-- root -->
  <xsl:template match="//tei:body">
    <xsl:text disable-output-escaping="yes">&lt;!DOCTYPE html&gt;</xsl:text>
    <html>
      <head>
        <meta charset="utf-8"/>
        <meta name="viewport" content="width=device-width, initial-scale=1"/>
        <link rel="stylesheet" href="read.css"/>
      </head>
      <body>
        <article class="rendition">
          <xsl:apply-templates/>
        </article>
      </body>
    </html>
  </xsl:template>
</xsl:stylesheet>
